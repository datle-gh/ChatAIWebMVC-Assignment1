namespace BusinessLogic.DTOs.Responses;

public sealed record StudentDashboardDto(
    int SubjectCount,
    int ChatSessionCount,
    int IndexedDocumentCount,
    IReadOnlyList<RecentCourseDto> RecentCourses);
