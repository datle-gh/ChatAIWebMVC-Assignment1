namespace BusinessLogic.DTOs.Requests;

public sealed record CreateSubjectRequestDto(
    string SubjectCode,
    string SubjectName,
    string? Description,
    int CreatedBy,
    string? CreatorRole = null);
