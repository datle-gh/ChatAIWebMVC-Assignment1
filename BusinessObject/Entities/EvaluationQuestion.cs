using System;
using System.Collections.Generic;

namespace BusinessObject.Entities;

public partial class EvaluationQuestion
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public string Question { get; set; } = null!;

    public string GroundTruthAnswer { get; set; } = null!;

    public int? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User? CreatedByNavigation { get; set; }

    public virtual ICollection<RagasBenchmarkResult> RagasBenchmarkResults { get; set; } = new List<RagasBenchmarkResult>();

    public virtual Subject Subject { get; set; } = null!;
}
