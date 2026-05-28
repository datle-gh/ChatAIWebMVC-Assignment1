using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Infrastructure;

public interface IUploadProgressReporter
{
    Task ReportAsync(
        UploadProgressDto progress,
        CancellationToken cancellationToken = default);
}
