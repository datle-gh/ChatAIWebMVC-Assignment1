namespace Presentation.Models;

public sealed class ChatMessageViewModel
{
    public int Id { get; set; }

    public string Role { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    public string? ModelName { get; set; }

    public DateTime CreatedAt { get; set; }

    public IReadOnlyList<CitationViewModel> Citations { get; set; } = [];
}
