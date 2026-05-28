using System.Text.Json;
using BusinessLogic.DTOs.Requests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BusinessLogic.Services;

public sealed class SystemSettingsService : ISystemSettingsService
{
    private readonly string _filePath;
    private readonly ILogger<SystemSettingsService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SystemSettingsService(
        IOptions<SystemSettingsFilePathOptions> options,
        ILogger<SystemSettingsService> logger)
    {
        _filePath = options.Value.FilePath;
        _logger = logger;
    }

    public async Task<SystemSettingsDto> GetSettingsAsync(
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath))
        {
            return new SystemSettingsDto();
        }

        try
        {
            var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            return JsonSerializer.Deserialize<SystemSettingsDto>(json, JsonOptions)
                   ?? new SystemSettingsDto();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to read settings from {FilePath}", _filePath);
            return new SystemSettingsDto();
        }
    }

    public async Task SaveSettingsAsync(
        SystemSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        await File.WriteAllTextAsync(_filePath, json, cancellationToken);

        _logger.LogInformation("System settings saved to {FilePath}", _filePath);
    }

    public async Task<(bool Success, string Message)> TestConnectionAsync(
        string provider,
        string apiKey,
        string model,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return (false, "Vui lòng nhập API Key.");
        }

        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };

            if (provider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}?key={apiKey}";
                var response = await httpClient.GetAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Kết nối Gemini API thành công!");
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return (false, $"Lỗi Gemini API: {response.StatusCode}. {body}");
            }

            if (provider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "https://api.openai.com/v1/models");
                request.Headers.Add("Authorization", $"Bearer {apiKey}");

                var response = await httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Kết nối OpenAI API thành công!");
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                return (false, $"Lỗi OpenAI API: {response.StatusCode}. {body}");
            }

            return (false, $"Provider '{provider}' không được hỗ trợ kiểm tra kết nối.");
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Test connection failed for {Provider}", provider);
            return (false, $"Không thể kết nối: {exception.Message}");
        }
    }
}
