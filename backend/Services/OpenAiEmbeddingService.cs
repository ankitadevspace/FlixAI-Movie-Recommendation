using Microsoft.Extensions.AI;

namespace NetflixClone.Services;

public sealed class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly IEmbeddingGenerator<string, Embedding<float>> _generator;

    public OpenAiEmbeddingService(
        IEmbeddingGenerator<string, Embedding<float>> generator)
    {
        _generator = generator;
    }

    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException(
                "Text cannot be empty.",
                nameof(text));
        }

        var embedding = await _generator.GenerateAsync(
            text,
            cancellationToken: cancellationToken);

        return embedding.Vector.ToArray();
    }
}