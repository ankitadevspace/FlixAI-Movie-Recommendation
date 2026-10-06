using NetflixClone.Dtos;

namespace NetflixClone.Services;

public interface IRagService
{
    Task<RagResponse> AskAsync(
        RagRequest request,
        CancellationToken cancellationToken = default);
}