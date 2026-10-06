using backend.Data;
using Microsoft.EntityFrameworkCore;
using NetflixClone.Dtos;
using Pgvector;
using Pgvector.EntityFrameworkCore;

namespace NetflixClone.Services;

public sealed class MovieSearchService : IMovieSearchService
{
    private readonly AppDbContext _context;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<MovieSearchService> _logger;

    public MovieSearchService(
        AppDbContext context,
        IEmbeddingService embeddingService,
        ILogger<MovieSearchService> logger)
    {
        _context = context;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<List<MovieSearchResult>> SearchAsync(
    string query,
    int count = 5,
    CancellationToken cancellationToken = default,
    double? similarityThreshold = null)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException(
                "Search query cannot be empty.",
                nameof(query));
        }

        count = Math.Clamp(count, 1, 20);

        _logger.LogInformation(
            "Generating search embedding for query: {Query}",
            query);

        var queryVector =
            await _embeddingService.GenerateEmbeddingAsync(
                query,
                cancellationToken);

        var vector = new Vector(queryVector);

        var results = await _context.MovieEmbeddings
            .AsNoTracking()
            .Include(x => x.Movie)
            .Where(x =>
                !string.IsNullOrWhiteSpace(x.Movie.Title) &&
                !string.IsNullOrWhiteSpace(x.Movie.Overview))
            .Select(x => new
            {
                Movie = x.Movie,
                Distance = x.Embedding!.CosineDistance(vector)
            })
            .OrderBy(x => x.Distance)
            .Take(count)
            .Select(x => new MovieSearchResult
            {
                MovieId = x.Movie.Id,
                Title = x.Movie.Title!,
                Overview = x.Movie.Overview,
                Genres = x.Movie.Genres,
                PosterPath = x.Movie.PosterPath,
                Rating = x.Movie.Rating,

                Similarity = 1.0 - x.Distance
            })
            .ToListAsync(cancellationToken);

        if (similarityThreshold.HasValue)
        {
            results = results
                .Where(x =>
                    x.Similarity >= similarityThreshold.Value)
                .ToList();
        }

        _logger.LogInformation(
            "Semantic search returned {Count} movies for query: {Query}",
            results.Count,
            query);

        return results;
    }
}