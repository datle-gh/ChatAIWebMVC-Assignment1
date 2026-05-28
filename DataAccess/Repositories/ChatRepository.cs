using BusinessObject.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public sealed class ChatRepository : IChatRepository
{
    private readonly ChatAIWebDbContext _context;

    public ChatRepository(ChatAIWebDbContext context)
    {
        _context = context;
    }

    public Task<ChatSession?> GetSessionByIdAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        return _context.ChatSessions
            .Include(session => session.Subject)
            .FirstOrDefaultAsync(session => session.Id == sessionId, cancellationToken);
    }

    public async Task<IReadOnlyList<ChatSession>> GetSessionsByUserAsync(
        int? userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ChatSessions
            .AsNoTracking()
            .Include(session => session.Subject)
            .Include(session => session.ChatMessages)
            .Where(session => session.UserId == userId)
            .OrderByDescending(session => session.UpdatedAt ?? session.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ChatSession>> GetSessionsByUserAndSubjectAsync(
        int? userId,
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ChatSessions
            .AsNoTracking()
            .Include(session => session.Subject)
            .Include(session => session.ChatMessages)
            .Where(session => session.UserId == userId && session.SubjectId == subjectId)
            .OrderByDescending(session => session.UpdatedAt ?? session.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ChatSession> CreateSessionAsync(
        ChatSession session,
        CancellationToken cancellationToken = default)
    {
        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync(cancellationToken);
        return session;
    }

    public async Task UpdateSessionAsync(
        ChatSession session,
        CancellationToken cancellationToken = default)
    {
        _context.ChatSessions.Update(session);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ChatMessage> AddMessageAsync(
        ChatMessage message,
        CancellationToken cancellationToken = default)
    {
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<IReadOnlyList<ChatMessage>> GetMessagesBySessionIdAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ChatMessages
            .AsNoTracking()
            .Where(message => message.ChatSessionId == sessionId)
            .OrderBy(message => message.CreatedAt)
            .ThenBy(message => message.Id)
            .ToListAsync(cancellationToken);
    }
}
