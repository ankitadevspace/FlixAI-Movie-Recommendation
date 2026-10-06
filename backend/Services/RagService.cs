using backend.Data;
using Microsoft.EntityFrameworkCore;
using NetflixClone.Dtos;

namespace NetflixClone.Services;

public sealed class RagService : IRagService
{
    private readonly AppDbContext _context;
    private readonly IUserAiContextService _userContextService;
    private readonly ISemanticSearchService _semanticSearchService;
    private readonly ILocalLlmService _localLlmService;
    private readonly ILogger<RagService> _logger;

    public RagService(
        AppDbContext context,
        IUserAiContextService userContextService,
        ISemanticSearchService semanticSearchService,
        ILocalLlmService localLlmService,
        ILogger<RagService> logger)
    {
        _context = context;
        _userContextService = userContextService;
        _semanticSearchService = semanticSearchService;
        _localLlmService = localLlmService;
        _logger = logger;
    }

    public async Task<RagResponse> AskAsync(
        RagRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var query = request.Query?.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException(
                "Query cannot be empty.",
                nameof(request.Query));
        }

        var count = Math.Clamp(request.Count, 1, 10);

        UserAiContext? userContext = null;

        if (request.UserId.HasValue)
        {
            userContext =
                await _userContextService.BuildContextAsync(
                    request.UserId.Value,
                    cancellationToken);
        }

        /*
         * ============================================================
         * RAG RETRIEVAL
         *
         * User query
         *      ↓
         * Embedding
         *      ↓
         * pgvector semantic search
         *      ↓
         * Candidate movies
         * ============================================================
         *
         * We intentionally retrieve more candidates than we finally
         * return. This gives the personalization layer enough movies
         * to work with.
         */

        var semanticCandidateCount =
            Math.Clamp(count * 4, count, 20);

        var semanticRequest = new SemanticSearchRequest
        {
            Query = query,
            Count = semanticCandidateCount
        };

        var semanticResponse =
            await _semanticSearchService.SearchAsync(
                semanticRequest,
                cancellationToken);

        /*
         * ============================================================
         * PERSONALIZED RE-RANKING
         *
         * Semantic similarity remains the primary signal.
         *
         * Additional signals:
         *   + preferred genres
         *   + favorite movies
         *   + highly rated movies
         *   - already watched movies
         *
         * The original Similarity value is NOT modified.
         * ============================================================
         */

        var rankedMovies = semanticResponse.Results
            .Select(movie => new
            {
                Movie = movie,
                PersonalizedScore =
                    CalculatePersonalizedScore(
                        movie,
                        userContext)
            })
            .OrderByDescending(x => x.PersonalizedScore)
            .Take(count)
            .Select(x => x.Movie)
            .ToList();

        var recommendations = rankedMovies
            .Select(movie => new RagRecommendation
            {
                MovieId = movie.MovieId,
                Title = movie.Title,
                Overview = movie.Overview,
                Genres = movie.Genres,
                PosterPath = movie.PosterPath,
                Rating = movie.Rating,
                Similarity = movie.Similarity,

                Reason = BuildReason(
                    query,
                    movie.Similarity,
                    movie.Genres,
                    movie.Rating)
            })
            .ToList();

        var sources = recommendations
            .Select(movie => new MovieSearchResult
            {
                MovieId = movie.MovieId,
                Title = movie.Title,
                Overview = movie.Overview,
                Genres = movie.Genres,
                PosterPath = movie.PosterPath,
                Rating = movie.Rating,
                Similarity = movie.Similarity
            })
            .ToList();

        /*
         * ============================================================
         * GENERATION
         *
         * Send the personalized retrieved context to Llama.
         * ============================================================
         */

        var answer = await GenerateAiAnswerAsync(
            query,
            recommendations,
            userContext,
            cancellationToken);

        _logger.LogInformation(
            "Personalized RAG request completed for query {Query}. " +
            "Retrieved {CandidateCount} candidates and returned {Count} recommendations.",
            query,
            semanticResponse.Results.Count,
            recommendations.Count);

        return new RagResponse
        {
            Query = query,
            UserId = request.UserId,
            Answer = answer,
            Recommendations = recommendations,
            Sources = sources,
            Mode = "Personalized RAG + pgvector + Ollama Llama 3.2",
            UserContext =
                userContext?.ToPromptContext()
                ?? "No user context supplied."
        };
    }

    private static double CalculatePersonalizedScore(
        SemanticSearchResult movie,
        UserAiContext? userContext)
    {
        /*
         * Without a user context, preserve semantic ranking.
         */
        if (userContext == null)
        {
            return movie.Similarity;
        }

        /*
         * ------------------------------------------------------------
         * Base semantic score
         * ------------------------------------------------------------
         *
         * 70% of the final score comes from semantic similarity.
         */
        var score = movie.Similarity * 0.70;

        /*
         * ------------------------------------------------------------
         * Preferred genre signal
         * ------------------------------------------------------------
         *
         * Up to 20%.
         */
        var genreMatch =
            CalculateGenreMatch(
                movie.Genres,
                userContext.PreferredGenres);

        score += genreMatch * 0.20;

        /*
         * ------------------------------------------------------------
         * Favorite movie signal
         * ------------------------------------------------------------
         *
         * Small boost when the recommendation itself is a favorite.
         */
        if (ContainsMovieTitle(
                userContext.FavoriteMovies,
                movie.Title))
        {
            score += 0.05;
        }

        /*
         * ------------------------------------------------------------
         * Highly-rated movie signal
         * ------------------------------------------------------------
         */
        if (ContainsMovieTitle(
                userContext.HighlyRatedMovies,
                movie.Title))
        {
            score += 0.05;
        }

        /*
         * ------------------------------------------------------------
         * Already watched penalty
         * ------------------------------------------------------------
         *
         * We don't want personalization to repeatedly recommend
         * movies the user has already watched.
         */
        if (ContainsMovieTitle(
                userContext.WatchedMovies,
                movie.Title))
        {
            score -= 0.15;
        }

        return score;
    }

    private static double CalculateGenreMatch(
        string? movieGenres,
        string? preferredGenres)
    {
        if (string.IsNullOrWhiteSpace(movieGenres) ||
            string.IsNullOrWhiteSpace(preferredGenres))
        {
            return 0;
        }

        var movieGenreNames =
            GetReadableGenresList(movieGenres);

        var preferredGenreNames =
            preferredGenres
                .Split(
                    ',',
                    StringSplitOptions.RemoveEmptyEntries |
                    StringSplitOptions.TrimEntries)
                .Select(NormalizeGenre)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        if (movieGenreNames.Count == 0 ||
            preferredGenreNames.Count == 0)
        {
            return 0;
        }

        var matchingGenres =
            movieGenreNames.Count(
                movieGenre =>
                    preferredGenreNames.Contains(
                        NormalizeGenre(movieGenre),
                        StringComparer.OrdinalIgnoreCase));

        return Math.Clamp(
            (double)matchingGenres /
            preferredGenreNames.Count,
            0,
            1);
    }

    private static bool ContainsMovieTitle(
        IEnumerable<string> movies,
        string? movieTitle)
    {
        if (string.IsNullOrWhiteSpace(movieTitle))
        {
            return false;
        }

        var normalizedTitle =
            NormalizeTitle(movieTitle);

        return movies.Any(movie =>
            NormalizeTitle(movie) == normalizedTitle);
    }

    private static string NormalizeTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return string.Empty;
        }

        return title
            .Trim()
            .ToLowerInvariant();
    }

    private static string NormalizeGenre(string? genre)
    {
        if (string.IsNullOrWhiteSpace(genre))
        {
            return string.Empty;
        }

        return genre
            .Trim()
            .ToLowerInvariant()
            .Replace("-", "")
            .Replace(" ", "");
    }

    private static List<string> GetReadableGenresList(
        string? genres)
    {
        if (string.IsNullOrWhiteSpace(genres))
        {
            return [];
        }

        var genreMap = new Dictionary<int, string>
        {
            [28] = "Action",
            [12] = "Adventure",
            [16] = "Animation",
            [35] = "Comedy",
            [80] = "Crime",
            [99] = "Documentary",
            [18] = "Drama",
            [10751] = "Family",
            [14] = "Fantasy",
            [36] = "History",
            [27] = "Horror",
            [10402] = "Music",
            [9648] = "Mystery",
            [10749] = "Romance",
            [878] = "Sci-Fi",
            [10770] = "TV Movie",
            [53] = "Thriller",
            [10752] = "War",
            [37] = "Western"
        };

        return genres
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Select(genre =>
            {
                if (int.TryParse(genre, out var id) &&
                    genreMap.TryGetValue(id, out var name))
                {
                    return name;
                }

                return genre;
            })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string GetReadableGenres(
        string? genres)
    {
        return string.Join(
            " • ",
            GetReadableGenresList(genres)
                .Take(3));
    }

    private async Task<string> GenerateAiAnswerAsync(
        string query,
        List<RagRecommendation> recommendations,
        UserAiContext? userContext,
        CancellationToken cancellationToken)
    {
        var movieContext = recommendations.Count == 0
            ? "No matching movies were retrieved from the FlixAI database."
            : string.Join(
                "\n\n",
                recommendations.Select((movie, index) =>
                    $"""
                    Movie {index + 1}:

                    Title: {movie.Title}

                    Overview: {movie.Overview ?? "Not available"}

                    Genres: {GetReadableGenres(movie.Genres)}

                    Rating: {(movie.Rating.HasValue
                        ? movie.Rating.Value.ToString("0.0")
                        : "Not available")}

                    Semantic Similarity: {movie.Similarity:0.0000}
                    """));

        var userContextText =
            userContext?.ToPromptContext()
            ?? "No user context supplied.";

        var prompt =
            $"""
            You are the AI movie recommendation assistant for FlixAI.

            The user asked:

            "{query}"

            Use ONLY the retrieved movie information and user context
            provided below.

            USER CONTEXT:
            {userContextText}

            RETRIEVED MOVIE CONTEXT:
            {movieContext}

            Instructions:

            - Answer the user's movie request naturally.
            - Recommend the most relevant movies from the retrieved context.
            - Explain briefly why the recommendations match the request.
            - Consider the user's preferences when user context is available.
            - Prefer movies that align with the user's preferred genres.
            - Avoid recommending movies that the user has already watched
              unless there is a strong reason to mention them.
            - Do not invent movies, ratings, genres, actors, plots, or facts.
            - If the retrieved context does not contain enough information,
              clearly say that.
            - Keep the answer concise and useful.
            - Use readable genre names such as "Sci-Fi", "Thriller",
              "Action", or "Drama".
            - Do not use raw genre IDs.
            - Do not mention embeddings, vectors, PostgreSQL,
              pgvector, RAG, or internal implementation details.
            - Only recommend movies present in the retrieved context.

            Write the final answer directly to the user.
            """;

        try
        {
            return await _localLlmService.GenerateAsync(
                prompt,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Ollama generation failed for query {Query}.",
                query);

            return BuildFallbackAnswer(
                query,
                recommendations,
                userContext);
        }
    }

    private static string BuildReason(
        string query,
        double similarity,
        string? genres,
        double? rating)
    {
        var genreNames =
            GetReadableGenres(genres);

        var reason =
            $"Matches the themes in your request: {query}.";

        if (!string.IsNullOrWhiteSpace(genreNames))
        {
            return
                $"{genreNames}\n" +
                $"{reason}";
        }

        return reason;
    }

    private static string BuildFallbackAnswer(
        string query,
        List<RagRecommendation> recommendations,
        UserAiContext? userContext)
    {
        if (recommendations.Count == 0)
        {
            return
                $"I couldn't find movies closely matching " +
                $"\"{query}\" in the current FlixAI library.";
        }

        var names = recommendations
            .Take(3)
            .Select(x => x.Title)
            .ToList();

        var answer =
            $"Based on \"{query}\", I found " +
            $"{recommendations.Count} relevant movies. ";

        answer +=
            $"The strongest matches are " +
            $"{string.Join(", ", names)}.";

        if (userContext != null &&
            !string.IsNullOrWhiteSpace(
                userContext.PreferredGenres))
        {
            answer +=
                $" I also considered the user's preferred genres: " +
                $"{userContext.PreferredGenres}.";
        }

        return answer;
    }
}