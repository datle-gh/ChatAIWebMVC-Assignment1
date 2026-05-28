namespace BusinessLogic.DTOs.Requests;

public sealed record ChatRequestDto(
    int SubjectId,
    string Question,
    int? ChatSessionId = null,
    int? UserId = null,
    int? TopK = null);
