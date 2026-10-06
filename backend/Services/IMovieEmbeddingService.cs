namespace NetflixClone.Services;

public interface IMovieEmbeddingService
{
    Task<int> IndexMoviesAsync(
        CancellationToken cancellationToken = default);
}