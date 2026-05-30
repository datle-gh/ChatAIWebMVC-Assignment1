namespace BusinessLogic.DTOs.Responses;

public sealed record EvaluationQuestionDto(
    int Id,
    int SubjectId,
    string SubjectName,
    string Question,
    string GroundTruthAnswer,
    string? CreatedByName,
    DateTime CreatedAt);

public sealed record RagasBenchmarkResultDto(
    int Id,
    int EvaluationQuestionId,
    string RunId,
    string Question,
    string? GroundTruthAnswer,
    string EmbeddingModel,
    string? LlmModel,
    string? VectorStore,
    string ChunkingStrategy,
    string? GeneratedAnswer,
    string? RetrievedContextsJson,
    decimal? Faithfulness,
    decimal? AnswerRelevancy,
    decimal? ContextPrecision,
    decimal? ContextRecall,
    decimal? OverallScore,
    DateTime CreatedAt);

public sealed record RagasRunSummaryDto(
    int SubjectId,
    string SubjectName,
    string EmbeddingModel,
    string? LlmModel,
    string ChunkingStrategy,
    int QuestionCount,
    decimal AvgFaithfulness,
    decimal AvgAnswerRelevancy,
    decimal AvgContextPrecision,
    decimal AvgContextRecall,
    decimal AvgOverallScore,
    DateTime RunDate,
    IReadOnlyList<RagasModelSummaryDto> ModelSummaries,
    IReadOnlyList<RagasBenchmarkResultDto> Results);

public sealed record RagasModelSummaryDto(
    string EmbeddingModel,
    string? LlmModel,
    string? VectorStore,
    string ChunkingStrategy,
    int QuestionCount,
    decimal AvgFaithfulness,
    decimal AvgAnswerRelevancy,
    decimal AvgContextPrecision,
    decimal AvgContextRecall,
    decimal AvgOverallScore);

public sealed record BenchmarkChunkingStrategyDto(
    string Key,
    string DisplayName,
    string Description,
    bool IsDefault);

public sealed record SubjectEvaluationSummaryDto(
    int SubjectId,
    string SubjectCode,
    string SubjectName,
    int QuestionCount,
    int BenchmarkRunCount,
    decimal? LastOverallScore,
    DateTime? LastRunDate);
