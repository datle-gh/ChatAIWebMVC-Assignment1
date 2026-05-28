namespace Presentation.Models;

public sealed class ChatPageViewModel
{
    public ChatAskViewModel Ask { get; set; } = new();

    public int? ChatSessionId { get; set; }

    public IReadOnlyList<ChatMessageViewModel> Messages { get; set; } = [];

    public IReadOnlyList<ChatSessionListItemViewModel> RecentSessions { get; set; } = [];

    public IReadOnlyList<SubjectOptionViewModel> Subjects { get; set; } = [];
}
