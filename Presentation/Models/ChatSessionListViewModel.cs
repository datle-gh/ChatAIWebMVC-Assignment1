namespace Presentation.Models;

public sealed class ChatSessionListViewModel
{
    public int? SubjectId { get; set; }

    public IReadOnlyList<ChatSessionListItemViewModel> Sessions { get; set; } = [];
}
