using BusinessObject.Entities;

namespace DataAccess.Repositories;

public interface IChatRepository
{
    Task<ChatSession?> GetSessionByIdAsync(int sessionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatSession>> GetSessionsByUserAsync(int? userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatSession>> GetSessionsByUserAndSubjectAsync(
        int? userId,
        int subjectId,
        CancellationToken cancellationToken = default);

    Task<ChatSession> CreateSessionAsync(ChatSession session, CancellationToken cancellationToken = default);

    Task UpdateSessionAsync(ChatSession session, CancellationToken cancellationToken = default);

    Task<ChatMessage> AddMessageAsync(ChatMessage message, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ChatMessage>> GetMessagesBySessionIdAsync(
        int sessionId,
        CancellationToken cancellationToken = default);
}
