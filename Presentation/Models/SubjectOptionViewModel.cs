namespace Presentation.Models;

public sealed class SubjectOptionViewModel
{
    public int Id { get; set; }

    public string SubjectCode { get; set; } = string.Empty;

    public string SubjectName { get; set; } = string.Empty;

    public string DisplayName => string.IsNullOrWhiteSpace(SubjectCode)
        ? SubjectName
        : $"{SubjectCode} - {SubjectName}";
}
