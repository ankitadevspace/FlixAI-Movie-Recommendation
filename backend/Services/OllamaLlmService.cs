using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NetflixClone.Services;

public sealed class OllamaLlmService : ILocalLlmService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaLlmService> _logger;

    public OllamaLlmService(
        HttpClient httpClient,
        ILogger<OllamaLlmService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GenerateAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException(
                "Prompt cannot be empty.",
                nameof(prompt));
        }

        var request = new OllamaChatRequest
        {
            Model = "llama3.2",
            Stream = false,
            Messages =
            [
                new OllamaMessage
                {
                    Role = "user",
                    Content = prompt
                }
            ]
        };

        _logger.LogInformation(
            "Sending prompt to local Ollama LLM.");

        using var response = await _httpClient.PostAsJsonAsync(
            "api/chat",
            request,
            cancellationToken);

        var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Ollama request failed with status {StatusCode}: {Response}",
                response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"Ollama returned {(int)response.StatusCode}: {responseBody}");
        }

        var result = JsonSerializer.Deserialize<OllamaChatResponse>(
            responseBody,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        if (result?.Message?.Content == null)
        {
            throw new InvalidOperationException(
                "Ollama returned an empty response.");
        }

        return result.Message.Content.Trim();
    }

    private sealed class OllamaChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<OllamaMessage> Messages { get; set; } = [];

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }
    }

    private sealed class OllamaMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private sealed class OllamaChatResponse
    {
        [JsonPropertyName("message")]
        public OllamaMessage? Message { get; set; }
    }
}