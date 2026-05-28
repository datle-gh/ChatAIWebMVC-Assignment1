namespace BusinessLogic.DTOs.Responses;

public sealed record RecentCourseDto(
    int Id,
    string SubjectCode,
    string SubjectName,
    string? Description,
    int DocumentCount,
    int IndexedDocumentCount,
    int ChatSessionCount,
    int ProgressPercent);
