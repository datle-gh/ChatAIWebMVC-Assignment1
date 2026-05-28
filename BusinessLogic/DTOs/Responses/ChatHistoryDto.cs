namespace BusinessLogic.DTOs.Responses;

public sealed record ChatHistoryDto(
    ChatSessionSummaryDto Session,
    IReadOnlyList<ChatMessageDto> Messages);
