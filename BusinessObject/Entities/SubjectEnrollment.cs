using System;
using System.Collections.Generic;

namespace BusinessObject.Entities;

public partial class SubjectEnrollment
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public int UserId { get; set; }

    public string RoleInClass { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual Subject Subject { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
