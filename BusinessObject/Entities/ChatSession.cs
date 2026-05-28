using System;
using System.Collections.Generic;

namespace BusinessObject.Entities;

public partial class ChatSession
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? SubjectId { get; set; }

    public string? Title { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();

    public virtual Subject? Subject { get; set; }

    public virtual User? User { get; set; }
}
