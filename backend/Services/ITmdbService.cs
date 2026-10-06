namespace NetflixClone.Services;

public interface ITmdbService
{
    Task<string> GetTrendingAsync(
        CancellationToken cancellationToken = default);

    Task<string> GetTopRatedAsync(
        CancellationToken cancellationToken = default);

    Task<string> GetMoviesByGenreAsync(
        int genreId,
        CancellationToken cancellationToken = default);

    Task<string> SearchMoviesAsync(
        string query,
        CancellationToken cancellationToken = default);

    Task<string> GetMovieDetailsAsync(
        int movieId,
        CancellationToken cancellationToken = default);

    Task<string> GetMovieVideosAsync(
        int movieId,
        CancellationToken cancellationToken = default);

    Task<string> SearchTvShowsAsync(
        string query,
        CancellationToken cancellationToken = default);

    Task<string> GetTvShowDetailsAsync(
        int tvId,
        CancellationToken cancellationToken = default);
}