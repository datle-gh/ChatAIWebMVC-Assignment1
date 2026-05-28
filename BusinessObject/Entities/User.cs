using System;
using System.Collections.Generic;

namespace BusinessObject.Entities;

public partial class User
{
    public int Id { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();

    public virtual ICollection<Document> Documents { get; set; } = new List<Document>();

    public virtual ICollection<EvaluationQuestion> EvaluationQuestions { get; set; } = new List<EvaluationQuestion>();

    public virtual ICollection<SubjectEnrollment> SubjectEnrollments { get; set; } = new List<SubjectEnrollment>();

    public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
