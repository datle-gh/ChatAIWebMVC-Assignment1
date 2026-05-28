using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;
using BusinessLogic.Infrastructure;
using BusinessObject.Entities;
using BusinessObject.Enums;
using DataAccess.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services;

public sealed class ChatbotService : IChatbotService
{
    private const string NotFoundAnswer = "Không tìm thấy thông tin liên quan trong tài liệu đã tải lên.";

    private readonly IEmbeddingModelRegistry _embeddingModelRegistry;
    private readonly IEmbeddingBackfillService _embeddingBackfillService;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly ILlmService _llmService;
    private readonly PromptBuilder _promptBuilder;
    private readonly IChatRepository _chatRepository;
    private readonly ICitationRepository _citationRepository;
    private readonly RagSettings _ragSettings;
    private readonly ILogger<ChatbotService> _logger;

    public ChatbotService(
        IEmbeddingModelRegistry embeddingModelRegistry,
        IEmbeddingBackfillService embeddingBackfillService,
        IVectorSearchService vectorSearchService,
        ILlmService llmService,
        PromptBuilder promptBuilder,
        IChatRepository chatRepository,
        ICitationRepository citationRepository,
        IConfiguration configuration,
        ILogger<ChatbotService> logger)
    {
        _embeddingModelRegistry = embeddingModelRegistry;
        _embeddingBackfillService = embeddingBackfillService;
        _vectorSearchService = vectorSearchService;
        _llmService = llmService;
        _promptBuilder = promptBuilder;
        _chatRepository = chatRepository;
        _citationRepository = citationRepository;
        _ragSettings = RagSettings.FromConfiguration(configuration);
        _logger = logger;
    }

    public async Task<ChatResponseDto> AskAsync(
        ChatRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ValidateRequest(request);

            var session = await GetOrCreateSessionAsync(request, cancellationToken);

            await _chatRepository.AddMessageAsync(
                new ChatMessage
                {
                    ChatSessionId = session.Id,
                    Role = ChatRole.User,
                    Content = request.Question.Trim(),
                    CreatedAt = DateTime.UtcNow
                },
                cancellationToken);

            var embeddingService = _embeddingModelRegistry.GetDefault();
            await _embeddingBackfillService.BackfillSubjectAsync(
                request.SubjectId,
                embeddingService.ModelKey,
                cancellationToken);

            var questionEmbedding = await embeddingService.GenerateEmbeddingAsync(
                request.Question,
                cancellationToken);

            var retrievedChunks = await _vectorSearchService.SearchAsync(
                request.SubjectId,
                embeddingService.ModelKey,
                questionEmbedding,
                request.TopK ?? _ragSettings.TopK,
                cancellationToken);

            var relevantChunks = retrievedChunks
                .Where(chunk => chunk.SimilarityScore >= _ragSettings.SimilarityThreshold)
                .Take(_ragSettings.MaxContextChunks)
                .ToList();

            var answer = relevantChunks.Count == 0
                ? NotFoundAnswer
                : await _llmService.GenerateAnswerAsync(
                    _promptBuilder.Build(request.Question, relevantChunks),
                    cancellationToken);

            var assistantMessage = await _chatRepository.AddMessageAsync(
                new ChatMessage
                {
                    ChatSessionId = session.Id,
                    Role = ChatRole.Assistant,
                    Content = answer,
                    ModelName = _llmService.ModelName,
                    CreatedAt = DateTime.UtcNow
                },
                cancellationToken);

            if (relevantChunks.Count > 0)
            {
                await _citationRepository.AddRangeAsync(
                    relevantChunks.Select(chunk => new Citation
                    {
                        ChatMessageId = assistantMessage.Id,
                        DocumentId = chunk.DocumentId,
                        ChunkId = chunk.ChunkId,
                        PageNumber = chunk.PageNumber,
                        SlideNumber = chunk.SlideNumber,
                        SimilarityScore = chunk.SimilarityScore,
                        CreatedAt = DateTime.UtcNow
                    }),
                    cancellationToken);
            }

            session.UpdatedAt = DateTime.UtcNow;
            await _chatRepository.UpdateSessionAsync(session, cancellationToken);

            return new ChatResponseDto(
                true,
                session.Id,
                assistantMessage.Id,
                answer,
                CreateCitationResponses(relevantChunks));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Chatbot question failed for subject {SubjectId}", request.SubjectId);

            return new ChatResponseDto(
                false,
                null,
                null,
                "Có lỗi khi xử lý câu hỏi.",
                [],
                exception is InvalidOperationException ? exception.Message : "Có lỗi khi gọi mô hình AI.");
        }
    }

    private async Task<ChatSession> GetOrCreateSessionAsync(
        ChatRequestDto request,
        CancellationToken cancellationToken)
    {
        if (request.ChatSessionId is null)
        {
            return await _chatRepository.CreateSessionAsync(
                new ChatSession
                {
                    UserId = request.UserId,
                    SubjectId = request.SubjectId,
                    Title = CreateSessionTitle(request.Question),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                cancellationToken);
        }

        var session = await _chatRepository.GetSessionByIdAsync(
            request.ChatSessionId.Value,
            cancellationToken);

        if (session is null)
        {
            throw new InvalidOperationException("Không tìm thấy phiên hội thoại.");
        }

        if (session.UserId != request.UserId)
        {
            throw new InvalidOperationException("Bạn không có quyền truy cập phiên hội thoại này.");
        }

        if (session.SubjectId != request.SubjectId)
        {
            throw new InvalidOperationException("Phiên hội thoại không thuộc môn học đã chọn.");
        }

        return session;
    }

    private static void ValidateRequest(ChatRequestDto request)
    {
        if (request.SubjectId <= 0)
        {
            throw new InvalidOperationException("Vui lòng nhập mã môn học hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(request.Question))
        {
            throw new InvalidOperationException("Vui lòng nhập câu hỏi.");
        }
    }

    private static string CreateSessionTitle(string question)
    {
        var title = question.Trim();
        return title.Length <= 60 ? title : $"{title[..60]}...";
    }

    private IReadOnlyList<CitationResponseDto> CreateCitationResponses(
        IReadOnlyList<RetrievedChunkDto> chunks)
    {
        return chunks
            .Select((chunk, index) => new CitationResponseDto(
                index + 1,
                chunk.DocumentId,
                chunk.ChunkId,
                GetDocumentDisplayName(chunk),
                chunk.PageNumber,
                chunk.SlideNumber,
                chunk.ChunkIndex,
                chunk.SimilarityScore,
                CreateSnippet(chunk.Content)))
            .ToList();
    }

    private string CreateSnippet(string content)
    {
        var normalized = string.Join(' ', content.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= _ragSettings.MaxCitationSnippetLength
            ? normalized
            : $"{normalized[.._ragSettings.MaxCitationSnippetLength]}...";
    }

    private static string GetDocumentDisplayName(RetrievedChunkDto chunk)
    {
        return !string.IsNullOrWhiteSpace(chunk.DocumentTitle)
            ? chunk.DocumentTitle
            : chunk.OriginalFileName;
    }
}
