using backend.Dtos;

namespace NetflixClone.Services;

public interface IRecommendationService
{
    Task<RecommendationResult> GetRecommendationsAsync(
        int userId,
        int count,
        CancellationToken cancellationToken = default);
}