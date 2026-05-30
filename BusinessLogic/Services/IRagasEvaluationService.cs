using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Services;

public interface IRagasEvaluationService
{
    Task<IReadOnlyList<SubjectEvaluationSummaryDto>> GetSubjectSummariesAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EvaluationQuestionDto>> GetQuestionsAsync(
        int subjectId,
        CancellationToken cancellationToken = default);

    IReadOnlyList<BenchmarkChunkingStrategyDto> GetChunkingStrategies();

    Task<OperationResult> AddQuestionAsync(
        int subjectId,
        string question,
        string groundTruthAnswer,
        int createdBy,
        CancellationToken cancellationToken = default);

    Task<OperationResult> UpdateQuestionAsync(
        int id,
        string question,
        string groundTruthAnswer,
        CancellationToken cancellationToken = default);

    Task<OperationResult> DeleteQuestionAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task<OperationResult> SeedQuestionsAsync(
        int subjectId,
        int createdBy,
        CancellationToken cancellationToken = default);

    Task<RagasRunSummaryDto?> RunEvaluationAsync(
        int subjectId,
        IReadOnlyList<string>? embeddingModels = null,
        IReadOnlyList<string>? chunkingStrategies = null,
        CancellationToken cancellationToken = default);

    Task<RagasRunSummaryDto?> GetLatestRunAsync(
        int subjectId,
        CancellationToken cancellationToken = default);
}
