using BusinessLogic.DTOs.Responses;
using BusinessLogic.Infrastructure;
using Microsoft.AspNetCore.SignalR;
using Presentation.Hubs;

namespace Presentation.Services;

public sealed class SignalRUploadProgressReporter : IUploadProgressReporter
{
    private readonly IHubContext<UploadProgressHub> _hubContext;

    public SignalRUploadProgressReporter(IHubContext<UploadProgressHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task ReportAsync(
        UploadProgressDto progress,
        CancellationToken cancellationToken = default)
    {
        return _hubContext.Clients
            .Group(UploadProgressGroups.ForUserUpload(progress.UserId, progress.UploadId))
            .SendAsync("UploadProgressUpdated", progress, cancellationToken);
    }
}
