using System;
using System.Collections.Generic;

namespace BusinessObject.Entities;

public partial class RagasBenchmarkResult
{
    public int Id { get; set; }

    public int EvaluationQuestionId { get; set; }

    public string RunId { get; set; } = null!;

    public string EmbeddingModel { get; set; } = null!;

    public string? LlmModel { get; set; }

    public string? VectorStore { get; set; }

    public string ChunkingStrategy { get; set; } = null!;

    public string? GeneratedAnswer { get; set; }

    public string? RetrievedContextsJson { get; set; }

    public decimal? Faithfulness { get; set; }

    public decimal? AnswerRelevancy { get; set; }

    public decimal? ContextPrecision { get; set; }

    public decimal? ContextRecall { get; set; }

    public decimal? OverallScore { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual EvaluationQuestion EvaluationQuestion { get; set; } = null!;
}
