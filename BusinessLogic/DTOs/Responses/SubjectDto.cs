namespace BusinessLogic.DTOs.Responses;

public sealed record SubjectDto(
    int Id,
    string SubjectCode,
    string SubjectName,
    string? Description,
    int DocumentCount,
    int IndexedDocumentCount,
    int StudentCount,
    int TeacherCount,
    DateTime CreatedAt,
    IReadOnlyList<string> TeacherNames);
