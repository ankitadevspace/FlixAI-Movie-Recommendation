using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace NetflixClone.Services;

public sealed class MovieEmbeddingService : IMovieEmbeddingService
{
    private readonly AppDbContext _context;
    private readonly IEmbeddingService _embeddingService;
    private readonly MovieDocumentService _documentService;
    private readonly ILogger<MovieEmbeddingService> _logger;

    public MovieEmbeddingService(
        AppDbContext context,
        IEmbeddingService embeddingService,
        MovieDocumentService documentService,
        ILogger<MovieEmbeddingService> logger)
    {
        _context = context;
        _embeddingService = embeddingService;
        _documentService = documentService;
        _logger = logger;
    }

    public async Task<int> IndexMoviesAsync(
        CancellationToken cancellationToken = default)
    {
        var movies = await _context.Movies
            .AsNoTracking()
            .Where(m =>
                !string.IsNullOrWhiteSpace(m.Title) &&
                !string.IsNullOrWhiteSpace(m.Overview))
            .ToListAsync(cancellationToken);

        var existingMovieIds = await _context.MovieEmbeddings
            .Select(x => x.MovieId)
            .ToHashSetAsync(cancellationToken);

        var indexed = 0;

        foreach (var movie in movies)
        {
            if (existingMovieIds.Contains(movie.Id))
            {
                continue;
            }

            var document = _documentService.BuildDocument(movie);

            if (string.IsNullOrWhiteSpace(document))
            {
                continue;
            }

            _logger.LogInformation(
                "Generating embedding for movie {MovieId}: {Title}",
                movie.Id,
                movie.Title);

            var vector = await _embeddingService
                .GenerateEmbeddingAsync(
                    document,
                    cancellationToken);

            var entity = new MovieEmbedding
            {
                MovieId = movie.Id,
                Content = document,
                Embedding = new Vector(vector),
                CreatedAt = DateTime.UtcNow
            };

            _context.MovieEmbeddings.Add(entity);

            indexed++;

            await _context.SaveChangesAsync(
                cancellationToken);
        }

        return indexed;
    }
}