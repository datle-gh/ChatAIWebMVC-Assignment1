using BusinessObject.Enums;

namespace Presentation.Models;

public sealed class DocumentIndexViewModel
{
    public string? SearchTerm { get; set; }

    public int? SubjectId { get; set; }

    public DocumentStatus? Status { get; set; }

    public IReadOnlyList<SubjectOptionViewModel> Subjects { get; set; } = [];

    public IReadOnlyList<DocumentListItemViewModel> Documents { get; set; } = [];
}
