using backend.Data;
using backend.Dtos;
using Microsoft.EntityFrameworkCore;

namespace NetflixClone.Services;

public sealed class RecommendationService : IRecommendationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<RecommendationService> _logger;

    public RecommendationService(
        AppDbContext context,
        ILogger<RecommendationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RecommendationResult> GetRecommendationsAsync(
        int userId,
        int count,
        CancellationToken cancellationToken = default)
    {
        // --------------------------------------------------------
        // 1. Validate input
        // --------------------------------------------------------

        if (count < 1)
        {
            count = 10;
        }

        if (count > 50)
        {
            count = 50;
        }

        // --------------------------------------------------------
        // 2. Validate user
        // --------------------------------------------------------

        var userExists = await _context.Users
            .AsNoTracking()
            .AnyAsync(
                u => u.Id == userId,
                cancellationToken);

        if (!userExists)
        {
            throw new KeyNotFoundException(
                $"User with ID {userId} was not found.");
        }

        // --------------------------------------------------------
        // 3. Get user's ratings
        // --------------------------------------------------------

        var ratings = await _context.Ratings
            .AsNoTracking()
            .Where(r => r.UserId == userId)
            .Select(r => new
            {
                r.MovieId,
                r.Score
            })
            .ToListAsync(cancellationToken);

        // --------------------------------------------------------
        // 4. Get user's favorites
        // --------------------------------------------------------

        var favoriteMovieIds = await _context.Favorites
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .Select(f => f.MovieId)
            .ToListAsync(cancellationToken);

        // --------------------------------------------------------
        // 5. Get user's watch history
        // --------------------------------------------------------

        var watchedMovieIds = await _context.WatchHistory
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Select(w => w.MovieId)
            .Distinct()
            .ToListAsync(cancellationToken);

        // --------------------------------------------------------
        // 6. Determine preferred genres
        // --------------------------------------------------------

        var preferredGenres = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);

        // Genres from movies the user rated highly
        var highlyRatedMovieIds = ratings
            .Where(r => r.Score >= 4)
            .Select(r => r.MovieId)
            .ToList();

        if (highlyRatedMovieIds.Count > 0)
        {
            var ratedGenres = await _context.Movies
                .AsNoTracking()
                .Where(m =>
                    highlyRatedMovieIds.Contains(m.Id))
                .Select(m => m.Genres)
                .ToListAsync(cancellationToken);

            AddGenres(
                ratedGenres,
                preferredGenres);
        }

        // Genres explicitly selected by the user
        var userGenres = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => u.PreferredGenres)
            .FirstOrDefaultAsync(cancellationToken);

        AddGenres(
            new[] { userGenres },
            preferredGenres);

        // --------------------------------------------------------
        // 7. Exclude movies already interacted with
        // --------------------------------------------------------

        var excludedMovieIds = ratings
            .Select(r => r.MovieId)
            .Concat(favoriteMovieIds)
            .Concat(watchedMovieIds)
            .Distinct()
            .ToHashSet();

        // --------------------------------------------------------
        // 8. Load candidate movies
        // --------------------------------------------------------

        var movies = await _context.Movies
            .AsNoTracking()
            .Where(m =>
                !excludedMovieIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        // --------------------------------------------------------
        // 9. Score candidates
        // --------------------------------------------------------

        var recommendations = movies
            .Select(movie =>
            {
                var movieGenres =
                    ParseGenres(movie.Genres).ToList();

                var matchingGenres = movieGenres
                    .Where(g => preferredGenres.Contains(g))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // ------------------------------------------------
                // Genre score
                //
                // If the movie contains genres the user likes,
                // this contributes up to 65 points.
                // ------------------------------------------------

                double genreScore = 0;

                if (preferredGenres.Count > 0 &&
                    movieGenres.Count > 0)
                {
                    var matchingRatio =
                        (double)matchingGenres.Count /
                        movieGenres.Count;

                    genreScore =
                        Math.Min(matchingRatio, 1.0) * 65;
                }

                // ------------------------------------------------
                // Movie quality score
                //
                // TMDb rating is normally 0-10.
                // This contributes up to 35 points.
                // ------------------------------------------------

                double qualityScore = 0;

                if (movie.Rating.HasValue)
                {
                    var normalizedRating =
                        Math.Clamp(
                            movie.Rating.Value / 10.0,
                            0.0,
                            1.0);

                    qualityScore =
                        normalizedRating * 35;
                }

                // ------------------------------------------------
                // Final score
                // ------------------------------------------------

                double score;

                if (preferredGenres.Count > 0)
                {
                    score =
                        genreScore +
                        qualityScore;
                }
                else
                {
                    // No profile information available.
                    // Fall back to movie quality.
                    score = qualityScore;
                }

                // ------------------------------------------------
                // Human-readable recommendation reason
                // ------------------------------------------------

                var reasons = new List<string>();

                if (matchingGenres.Count > 0)
                {
                    reasons.Add(
                        $"matches your interest in " +
                        $"{string.Join(", ", matchingGenres.Take(2))}");
                }

                if (movie.Rating >= 8)
                {
                    reasons.Add("highly rated");
                }
                else if (movie.Rating >= 7)
                {
                    reasons.Add("well rated");
                }

                var reason = reasons.Count > 0
                    ? string.Join("; ", reasons)
                    : "recommended based on movie quality";

                return new RecommendationResponse
                {
                    MovieId = movie.Id,
                    TmdbId = movie.TmdbId,
                    Title = movie.Title,
                    Overview = movie.Overview,
                    PosterPath = movie.PosterPath,
                    BackdropPath = movie.BackdropPath,
                    Genres = movie.Genres,
                    Rating = movie.Rating,

                    RecommendationScore =
                        Math.Round(score, 2),

                    Reason = reason
                };
            })
            .OrderByDescending(
                x => x.RecommendationScore)
            .ThenByDescending(
                x => x.Rating ?? 0)
            .ThenBy(
                x => x.Title)
            .Take(count)
            .ToList();

        // --------------------------------------------------------
        // 10. Determine recommendation strategy
        // --------------------------------------------------------

        var strategy = preferredGenres.Count > 0
            ? "Content-based: preferred genres + movie quality"
            : "Fallback: movie quality";

        _logger.LogInformation(
            "Generated {Count} recommendations for user {UserId} " +
            "using {Strategy}. Preferred genres: {Genres}",
            recommendations.Count,
            userId,
            strategy,
            preferredGenres.Count > 0
                ? string.Join(", ", preferredGenres)
                : "none");

        // --------------------------------------------------------
        // 11. Return result
        // --------------------------------------------------------

        return new RecommendationResult
        {
            UserId = userId,
            Strategy = strategy,
            Recommendations = recommendations
        };
    }

    // ============================================================
    // ADD GENRES
    // ============================================================

    private static void AddGenres(
        IEnumerable<string?>? genreStrings,
        HashSet<string> genres)
    {
        if (genreStrings == null)
        {
            return;
        }

        foreach (var genreString in genreStrings)
        {
            if (string.IsNullOrWhiteSpace(genreString))
            {
                continue;
            }

            foreach (var genre in ParseGenres(genreString))
            {
                genres.Add(genre);
            }
        }
    }

    // ============================================================
    // PARSE GENRES
    // ============================================================

    private static IEnumerable<string> ParseGenres(
        string? genres)
    {
        if (string.IsNullOrWhiteSpace(genres))
        {
            return [];
        }

        return genres
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x));
    }
}