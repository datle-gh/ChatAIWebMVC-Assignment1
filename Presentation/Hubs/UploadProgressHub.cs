using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Presentation.Hubs;

[Authorize]
public sealed class UploadProgressHub : Hub
{
    public Task JoinUpload(string uploadId)
    {
        var userIdValue = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId) || userId <= 0 || string.IsNullOrWhiteSpace(uploadId))
        {
            throw new HubException("Phiên tải lên không hợp lệ.");
        }

        return Groups.AddToGroupAsync(Context.ConnectionId, UploadProgressGroups.ForUserUpload(userId, uploadId));
    }
}
