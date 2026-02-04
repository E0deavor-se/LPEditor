using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace LPEditorApp.Services.Ai;

public interface IAiChatClient
{
    Task<string> CreateChatCompletionAsync(string model, List<OpenAiMessage> messages, bool strictJsonOnly, CancellationToken cancellationToken);
}

public class OpenAiChatClient : IAiChatClient
{
    private readonly HttpClient _httpClient;
    private readonly AiOptions _options;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public OpenAiChatClient(HttpClient httpClient, IOptions<AiOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<string> CreateChatCompletionAsync(string model, List<OpenAiMessage> messages, bool strictJsonOnly, CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["model"] = string.IsNullOrWhiteSpace(model) ? _options.Model : model,
            ["temperature"] = 0.6,
            ["messages"] = messages
        };

        if (strictJsonOnly)
        {
            payload["response_format"] = new { type = "json_object" };
        }

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = JsonContent.Create(payload, options: JsonOptions)
        };

        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var trimmed = TrimLong(raw);
            throw new InvalidOperationException($"AI API request failed: {(int)response.StatusCode} {response.ReasonPhrase} {trimmed}");
        }

        var completion = JsonSerializer.Deserialize<OpenAiChatResponse>(raw, JsonOptions);
        var content = completion?.Choices?.FirstOrDefault()?.Message?.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException("AI response is empty.");
        }

        return content;
    }

    private static string TrimLong(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (value.Length <= 1200)
        {
            return value;
        }

        return value[..800] + "..." + value[^200..];
    }
}

public class OpenAiChatResponse
{
    [JsonPropertyName("choices")]
    public List<OpenAiChoice> Choices { get; set; } = new();
}

public class OpenAiChoice
{
    [JsonPropertyName("message")]
    public OpenAiMessage Message { get; set; } = new();
}

public class OpenAiMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
