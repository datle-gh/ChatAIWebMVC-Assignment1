namespace BusinessLogic.DTOs.Responses;

public sealed record ChatSessionSummaryDto(
    int Id,
    int? SubjectId,
    string? SubjectName,
    string Title,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int MessageCount);
