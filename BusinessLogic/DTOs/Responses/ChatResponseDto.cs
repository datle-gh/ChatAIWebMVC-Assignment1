namespace BusinessLogic.DTOs.Responses;

public sealed record ChatResponseDto(
    bool Succeeded,
    int? ChatSessionId,
    int? AssistantMessageId,
    string Answer,
    IReadOnlyList<CitationResponseDto> Citations,
    string? ErrorMessage = null);
