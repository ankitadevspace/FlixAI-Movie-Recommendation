namespace NetflixClone.Services;

public interface ILocalLlmService
{
    Task<string> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default);
}