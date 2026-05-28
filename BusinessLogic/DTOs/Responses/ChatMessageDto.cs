namespace BusinessLogic.DTOs.Responses;

public sealed record ChatMessageDto(
    int Id,
    int ChatSessionId,
    string Role,
    string Content,
    string? ModelName,
    DateTime CreatedAt,
    IReadOnlyList<CitationResponseDto> Citations);
