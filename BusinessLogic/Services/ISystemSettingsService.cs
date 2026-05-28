using BusinessLogic.DTOs.Requests;

namespace BusinessLogic.Services;

public interface ISystemSettingsService
{
    Task<SystemSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);

    Task SaveSettingsAsync(SystemSettingsDto settings, CancellationToken cancellationToken = default);

    Task<(bool Success, string Message)> TestConnectionAsync(
        string provider,
        string apiKey,
        string model,
        CancellationToken cancellationToken = default);
}
