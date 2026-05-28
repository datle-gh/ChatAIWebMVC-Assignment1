using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Infrastructure;

public sealed class NoopUploadProgressReporter : IUploadProgressReporter
{
    public Task ReportAsync(
        UploadProgressDto progress,
        CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
