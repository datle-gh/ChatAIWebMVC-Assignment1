using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;
using BusinessLogic.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services;

public sealed class GeminiLlmService : ILlmService
{
    private readonly HttpClient _httpClient;
    private readonly LlmSettings _settings;
    private readonly ILogger<GeminiLlmService> _logger;

    public GeminiLlmService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<GeminiLlmService> logger)
    {
        _httpClient = httpClient;
        _settings = LlmSettings.FromConfiguration(configuration);
        _logger = logger;

        if (_httpClient.BaseAddress is null)
        {
            _httpClient.BaseAddress = new Uri(NormalizeBaseUrl(_settings.Gemini.BaseUrl));
        }
    }

    public string ModelName => _settings.Model;

    public async Task<string> GenerateAnswerAsync(
        string prompt,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new InvalidOperationException("Prompt không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(_settings.Gemini.ApiKey))
        {
            throw new InvalidOperationException("Chưa cấu hình Gemini API key.");
        }

        try
        {
            var requestUri = $"/v1beta/models/{Uri.EscapeDataString(_settings.Model)}:generateContent?key={Uri.EscapeDataString(_settings.Gemini.ApiKey)}";
            var response = await _httpClient.PostAsJsonAsync(
                requestUri,
                CreateRequest(prompt),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var detail = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Gemini request failed with status {StatusCode}: {Detail}",
                    response.StatusCode,
                    detail);

                throw new InvalidOperationException("Có lỗi khi gọi mô hình AI.");
            }

            var payload = await response.Content.ReadFromJsonAsync<GeminiGenerateContentResponse>(
                cancellationToken: cancellationToken);
            var answer = ExtractText(payload);

            if (string.IsNullOrWhiteSpace(answer))
            {
                throw new InvalidOperationException("Mô hình AI không trả về nội dung.");
            }

            return answer.Trim();
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Cannot connect to Gemini API");
            throw new InvalidOperationException("Không thể kết nối mô hình AI.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(exception, "Gemini request timed out");
            throw new InvalidOperationException("Quá thời gian chờ khi gọi mô hình AI.", exception);
        }
    }

    private GeminiGenerateContentRequest CreateRequest(string prompt)
    {
        return new GeminiGenerateContentRequest(
            [
                new GeminiContent(
                    "user",
                    [new GeminiPart(prompt)])
            ],
            new GeminiGenerationConfig(
                _settings.Gemini.Temperature,
                _settings.Gemini.MaxOutputTokens));
    }

    private static string ExtractText(GeminiGenerateContentResponse? response)
    {
        var builder = new StringBuilder();
        var parts = response?.Candidates?
            .SelectMany(candidate => candidate.Content?.Parts ?? [])
            .Select(part => part.Text)
            .Where(text => !string.IsNullOrWhiteSpace(text));

        foreach (var part in parts ?? [])
        {
            if (builder.Length > 0)
            {
                builder.AppendLine();
            }

            builder.Append(part);
        }

        return builder.ToString();
    }

    private static string NormalizeBaseUrl(string baseUrl)
    {
        return string.IsNullOrWhiteSpace(baseUrl)
            ? "https://generativelanguage.googleapis.com"
            : baseUrl.TrimEnd('/');
    }

    private sealed record GeminiGenerateContentRequest(
        [property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents,
        [property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

    private sealed record GeminiContent(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string Text);

    private sealed record GeminiGenerationConfig(
        [property: JsonPropertyName("temperature")] double Temperature,
        [property: JsonPropertyName("maxOutputTokens")] int MaxOutputTokens);

    private sealed record GeminiGenerateContentResponse(
        [property: JsonPropertyName("candidates")] IReadOnlyList<GeminiCandidate>? Candidates);

    private sealed record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiContent? Content);
}
