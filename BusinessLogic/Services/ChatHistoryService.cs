using BusinessLogic.DTOs.Responses;
using BusinessLogic.Infrastructure;
using BusinessObject.Entities;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Services;

public sealed class ChatHistoryService : IChatHistoryService
{
    private readonly IChatRepository _chatRepository;
    private readonly ICitationRepository _citationRepository;
    private readonly RagSettings _ragSettings;

    public ChatHistoryService(
        IChatRepository chatRepository,
        ICitationRepository citationRepository,
        IConfiguration configuration)
    {
        _chatRepository = chatRepository;
        _citationRepository = citationRepository;
        _ragSettings = RagSettings.FromConfiguration(configuration);
    }

    public async Task<IReadOnlyList<ChatSessionSummaryDto>> GetSessionsAsync(
        int? userId,
        int? subjectId = null,
        CancellationToken cancellationToken = default)
    {
        var sessions = subjectId.HasValue && subjectId.Value > 0
            ? await _chatRepository.GetSessionsByUserAndSubjectAsync(userId, subjectId.Value, cancellationToken)
            : await _chatRepository.GetSessionsByUserAsync(userId, cancellationToken);

        return sessions.Select(CreateSessionSummary).ToList();
    }

    public async Task<ChatHistoryDto?> GetHistoryAsync(
        int sessionId,
        int? userId,
        CancellationToken cancellationToken = default)
    {
        var session = await _chatRepository.GetSessionByIdAsync(sessionId, cancellationToken);
        if (session is null || session.UserId != userId)
        {
            return null;
        }

        var messages = await _chatRepository.GetMessagesBySessionIdAsync(sessionId, cancellationToken);
        var citationsByMessage = await _citationRepository.GetByChatMessageIdsAsync(
            messages.Select(message => message.Id),
            cancellationToken);

        var messageDtos = messages
            .Select(message => new ChatMessageDto(
                message.Id,
                message.ChatSessionId,
                message.Role.ToString(),
                message.Content,
                message.ModelName,
                message.CreatedAt,
                CreateCitationResponses(citationsByMessage.GetValueOrDefault(message.Id, []))))
            .ToList();

        return new ChatHistoryDto(CreateSessionSummary(session), messageDtos);
    }

    private static ChatSessionSummaryDto CreateSessionSummary(ChatSession session)
    {
        return new ChatSessionSummaryDto(
            session.Id,
            session.SubjectId,
            session.Subject?.SubjectName,
            string.IsNullOrWhiteSpace(session.Title) ? "Phiên hội thoại" : session.Title,
            session.CreatedAt,
            session.UpdatedAt,
            session.ChatMessages?.Count ?? 0);
    }

    private IReadOnlyList<CitationResponseDto> CreateCitationResponses(
        IReadOnlyList<Citation> citations)
    {
        return citations
            .Select((citation, index) => new CitationResponseDto(
                index + 1,
                citation.DocumentId,
                citation.ChunkId,
                GetDocumentDisplayName(citation),
                citation.PageNumber,
                citation.SlideNumber,
                citation.Chunk.ChunkIndex,
                citation.SimilarityScore,
                CreateSnippet(citation.Chunk.Content)))
            .ToList();
    }

    private string CreateSnippet(string content)
    {
        var normalized = string.Join(' ', content.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= _ragSettings.MaxCitationSnippetLength
            ? normalized
            : $"{normalized[.._ragSettings.MaxCitationSnippetLength]}...";
    }

    private static string GetDocumentDisplayName(Citation citation)
    {
        return !string.IsNullOrWhiteSpace(citation.Document.Title)
            ? citation.Document.Title
            : citation.Document.OriginalFileName;
    }
}
