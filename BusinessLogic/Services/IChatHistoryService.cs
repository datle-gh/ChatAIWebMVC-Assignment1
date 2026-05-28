using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Services;

public interface IChatHistoryService
{
    Task<IReadOnlyList<ChatSessionSummaryDto>> GetSessionsAsync(
        int? userId,
        int? subjectId = null,
        CancellationToken cancellationToken = default);

    Task<ChatHistoryDto?> GetHistoryAsync(
        int sessionId,
        int? userId,
        CancellationToken cancellationToken = default);
}
