using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetflixClone.Services;

public sealed class OllamaEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaEmbeddingService> _logger;

    public OllamaEmbeddingService(
        HttpClient httpClient,
        ILogger<OllamaEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
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

        var request = new OllamaEmbeddingRequest
        {
            Model = "nomic-embed-text",
            Input = text
        };

        _logger.LogInformation(
            "Generating local embedding using Ollama.");

        using var response = await _httpClient.PostAsJsonAsync(
            "api/embed",
            request,
            cancellationToken);

        var responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Ollama embedding request failed. " +
                "Status: {StatusCode}, Response: {Response}",
                response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"Ollama embedding request failed: " +
                $"{(int)response.StatusCode} {responseBody}");
        }

        var result =
            JsonSerializer.Deserialize<OllamaEmbeddingResponse>(
                responseBody,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

        if (result?.Embeddings == null ||
            result.Embeddings.Count == 0)
        {
            throw new InvalidOperationException(
                "Ollama returned an empty embedding.");
        }

        var vector = result.Embeddings[0];

        _logger.LogInformation(
            "Generated embedding with {Dimensions} dimensions.",
            vector.Count);

        return vector.ToArray();
    }

    private sealed class OllamaEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;
    }

    private sealed class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embeddings")]
        public List<List<float>> Embeddings { get; set; } = [];
    }
}