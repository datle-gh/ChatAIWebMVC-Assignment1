namespace Presentation.Models;

public sealed class SelectSubjectViewModel
{
    public IReadOnlyList<SubjectSelectionItemViewModel> Subjects { get; set; } = [];

    public string SelectedFilter { get; set; } = "enrolled";

    public bool ShowTeacherFilters { get; set; }
}

public sealed class SubjectSelectionItemViewModel
{
    public int Id { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int IndexedDocumentCount { get; set; }
    public bool IsEnrolled { get; set; }
    public bool IsReady => IndexedDocumentCount > 0;
}
