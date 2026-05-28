namespace Presentation.Models;

public sealed class ChatSessionListItemViewModel
{
    public int Id { get; set; }

    public int? SubjectId { get; set; }

    public string? SubjectName { get; set; }

    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int MessageCount { get; set; }
}
