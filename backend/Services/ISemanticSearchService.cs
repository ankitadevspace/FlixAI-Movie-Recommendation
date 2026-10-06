using NetflixClone.Dtos;

namespace NetflixClone.Services;

public interface ISemanticSearchService
{
    Task<SemanticSearchResponse> SearchAsync(
        SemanticSearchRequest request,
        CancellationToken cancellationToken = default);
}