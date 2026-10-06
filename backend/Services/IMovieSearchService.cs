using NetflixClone.Dtos;

namespace NetflixClone.Services;

public interface IMovieSearchService
{
    Task<List<MovieSearchResult>> SearchAsync(
        string query,
        int count = 5,
        CancellationToken cancellationToken = default,
        double? similarityThreshold = null);
}