namespace Presentation.Models;

public sealed class DashboardViewModel
{
    public int SubjectCount { get; set; }
    public int ChatSessionCount { get; set; }
    public int IndexedDocumentCount { get; set; }
    public IReadOnlyList<RecentCourseViewModel> RecentCourses { get; set; } = [];
    public string UserName { get; set; } = string.Empty;
}

public sealed class RecentCourseViewModel
{
    public int Id { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DocumentCount { get; set; }
    public int IndexedDocumentCount { get; set; }
    public int ChatSessionCount { get; set; }
    public int ProgressPercent { get; set; }
}
