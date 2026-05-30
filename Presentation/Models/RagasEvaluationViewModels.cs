using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

public sealed class RagasSubjectListViewModel
{
    public List<RagasSubjectItem> Subjects { get; set; } = new();
}

public sealed class RagasSubjectItem
{
    public int SubjectId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public int BenchmarkRunCount { get; set; }
    public decimal? LastOverallScore { get; set; }
    public DateTime? LastRunDate { get; set; }
}

public sealed class RagasQuestionsViewModel
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public List<RagasEmbeddingModelOption> EmbeddingModels { get; set; } = new();
    public List<RagasChunkingStrategyOption> ChunkingStrategies { get; set; } = new();
    public List<RagasQuestionItem> Questions { get; set; } = new();
}

public sealed class RagasEmbeddingModelOption
{
    public string Key { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public sealed class RagasChunkingStrategyOption
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public sealed class RagasQuestionItem
{
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string GroundTruthAnswer { get; set; } = string.Empty;
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class RagasAddQuestionViewModel
{
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập câu hỏi.")]
    public string Question { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập câu trả lời chuẩn.")]
    public string GroundTruthAnswer { get; set; } = string.Empty;
}

public sealed class RagasEditQuestionViewModel
{
    public int Id { get; set; }
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập câu hỏi.")]
    public string Question { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập câu trả lời chuẩn.")]
    public string GroundTruthAnswer { get; set; } = string.Empty;
}

public sealed class RagasRunResultViewModel
{
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = string.Empty;
    public string? LlmModel { get; set; }
    public string ChunkingStrategy { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public decimal AvgFaithfulness { get; set; }
    public decimal AvgAnswerRelevancy { get; set; }
    public decimal AvgContextPrecision { get; set; }
    public decimal AvgContextRecall { get; set; }
    public decimal AvgOverallScore { get; set; }
    public DateTime RunDate { get; set; }
    public List<RagasModelSummaryItem> ModelSummaries { get; set; } = new();
    public List<RagasResultDetailItem> Results { get; set; } = new();
}

public sealed class RagasModelSummaryItem
{
    public string EmbeddingModel { get; set; } = string.Empty;
    public string? LlmModel { get; set; }
    public string? VectorStore { get; set; }
    public string ChunkingStrategy { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public decimal AvgFaithfulness { get; set; }
    public decimal AvgAnswerRelevancy { get; set; }
    public decimal AvgContextPrecision { get; set; }
    public decimal AvgContextRecall { get; set; }
    public decimal AvgOverallScore { get; set; }
}

public sealed class RagasResultDetailItem
{
    public string EmbeddingModel { get; set; } = string.Empty;
    public string ChunkingStrategy { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public string? GroundTruthAnswer { get; set; }
    public string? GeneratedAnswer { get; set; }
    public decimal? Faithfulness { get; set; }
    public decimal? AnswerRelevancy { get; set; }
    public decimal? ContextPrecision { get; set; }
    public decimal? ContextRecall { get; set; }
    public decimal? OverallScore { get; set; }
}
