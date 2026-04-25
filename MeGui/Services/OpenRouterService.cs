using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MeGui.Services;

public class OpenRouterSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "openai/gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://openrouter.ai/api/v1";
}

public class OpenRouterService
{
    private readonly HttpClient _httpClient;
    private readonly OpenRouterSettings _settings;
    private readonly ILogger<OpenRouterService> _logger;

    public OpenRouterService(HttpClient httpClient, OpenRouterSettings settings, ILogger<OpenRouterService> logger)
    {
        _httpClient = httpClient;
        _settings = settings;
        _logger = logger;

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
    }

    public async Task<string> ChatAsync(string systemPrompt, string userMessage, CancellationToken ct = default)
    {
        var request = new ChatCompletionRequest
        {
            Model = _settings.Model,
            Messages =
            [
                new ChatMessage { Role = "system", Content = systemPrompt },
                new ChatMessage { Role = "user", Content = userMessage }
            ]
        };

        var json = JsonSerializer.Serialize(request, JsonCtx.Default.ChatCompletionRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await _httpClient.PostAsync("/api/v1/chat/completions", content, ct);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize(responseJson, JsonCtx.Default.ChatCompletionResponse);

            return result?.Choices?.FirstOrDefault()?.Message?.Content ?? "Desculpe, não consegui processar sua mensagem.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao chamar OpenRouter");
            return "Desculpe, ocorreu um erro ao processar sua mensagem. Tente novamente.";
        }
    }
}

#region DTOs

public class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = [];
}

public class ChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public List<ChatChoice>? Choices { get; set; }
}

public class ChatChoice
{
    [JsonPropertyName("message")]
    public ChatMessage? Message { get; set; }
}

#endregion

[JsonSerializable(typeof(ChatCompletionRequest))]
[JsonSerializable(typeof(ChatCompletionResponse))]
internal partial class JsonCtx : JsonSerializerContext { }
