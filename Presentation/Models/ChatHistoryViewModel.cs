namespace Presentation.Models;

public sealed class ChatHistoryViewModel
{
    public ChatSessionListItemViewModel Session { get; set; } = new();

    public IReadOnlyList<ChatMessageViewModel> Messages { get; set; } = [];
}
