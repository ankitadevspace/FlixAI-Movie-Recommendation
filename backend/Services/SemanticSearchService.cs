using backend.Data;
using Microsoft.EntityFrameworkCore;
using NetflixClone.Dtos;
using Pgvector;

namespace NetflixClone.Services;

public sealed class SemanticSearchService : ISemanticSearchService
{
    private readonly AppDbContext _context;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<SemanticSearchService> _logger;

    public SemanticSearchService(
        AppDbContext context,
        IEmbeddingService embeddingService,
        ILogger<SemanticSearchService> logger)
    {
        _context = context;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<SemanticSearchResponse> SearchAsync(
        SemanticSearchRequest request,
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
                "Search query cannot be empty.",
                nameof(request.Query));
        }

        var count = Math.Clamp(request.Count, 1, 20);

        _logger.LogInformation(
            "Semantic movie search started for query: {Query}",
            query);

        // -------------------------------------------------
        // 1. Convert the user's search text into an embedding
        // -------------------------------------------------

        var embeddingValues =
            await _embeddingService.GenerateEmbeddingAsync(
                query,
                cancellationToken);

        var queryVector = new Vector(embeddingValues);

        // -------------------------------------------------
        // 2. Search PostgreSQL using pgvector
        //
        // <=> = cosine distance
        // Smaller distance = better match
        // -------------------------------------------------

        var embeddings = await _context.MovieEmbeddings
            .FromSqlInterpolated($"""
                SELECT *
                FROM "MovieEmbeddings"
                ORDER BY "Embedding" <=> {queryVector}
                LIMIT {count}
                """)
            .Include(x => x.Movie)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // -------------------------------------------------
        // 3. Convert cosine distance into similarity
        //
        // similarity = 1 - cosine distance
        // -------------------------------------------------

        var results = embeddings
            .Select(x =>
            {
                var similarity = CalculateCosineSimilarity(
                    queryVector,
                    x.Embedding);

                return new SemanticSearchResult
                {
                    MovieId = x.MovieId,

                    Title = x.Movie.Title ?? string.Empty,

                    Overview = x.Movie.Overview,

                    PosterPath = x.Movie.PosterPath,

                    BackdropPath = x.Movie.BackdropPath,

                    ReleaseDate = x.Movie.ReleaseDate,

                    Rating = x.Movie.Rating,

                    Genres = x.Movie.Genres,

                    Similarity = Math.Round(
                        similarity,
                        4),

                    Content = x.Content
                };
            })
            .ToList();

        _logger.LogInformation(
            "Semantic movie search completed. Found {Count} results.",
            results.Count);

        return new SemanticSearchResponse
        {
            Query = query,
            Count = results.Count,
            Results = results
        };
    }

    // -------------------------------------------------
    // Calculate cosine similarity in C#
    // -------------------------------------------------

    private static double CalculateCosineSimilarity(
        Vector first,
        Vector second)
    {
        var a = first.ToArray();
        var b = second.ToArray();

        if (a.Length != b.Length || a.Length == 0)
        {
            return 0;
        }

        double dotProduct = 0;
        double magnitudeA = 0;
        double magnitudeB = 0;

        for (var i = 0; i < a.Length; i++)
        {
            dotProduct += a[i] * b[i];

            magnitudeA += a[i] * a[i];

            magnitudeB += b[i] * b[i];
        }

        if (magnitudeA == 0 || magnitudeB == 0)
        {
            return 0;
        }

        return dotProduct /
               (Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB));
    }
}