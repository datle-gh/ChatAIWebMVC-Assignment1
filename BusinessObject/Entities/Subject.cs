using System;
using System.Collections.Generic;

namespace BusinessObject.Entities;

public partial class Subject
{
    public int Id { get; set; }

    public string SubjectCode { get; set; } = null!;

    public string SubjectName { get; set; } = null!;

    public string? Description { get; set; }

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<EvaluationQuestion> EvaluationQuestions { get; set; } = new List<EvaluationQuestion>();

    public virtual ICollection<SubjectEnrollment> SubjectEnrollments { get; set; } = new List<SubjectEnrollment>();
}
