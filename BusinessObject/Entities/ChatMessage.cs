using System;
using System.Collections.Generic;
using BusinessObject.Enums;

namespace BusinessObject.Entities;

public partial class ChatMessage
{
    public int Id { get; set; }

    public int ChatSessionId { get; set; }

    public ChatRole Role { get; set; }

    public string Content { get; set; } = null!;

    public string? ModelName { get; set; }

    public int? PromptTokens { get; set; }

    public int? CompletionTokens { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ChatSession ChatSession { get; set; } = null!;

    public virtual ICollection<Citation> Citations { get; set; } = new List<Citation>();
}
