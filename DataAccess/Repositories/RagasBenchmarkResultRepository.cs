using BusinessObject.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Repositories;

public sealed class RagasBenchmarkResultRepository : IRagasBenchmarkResultRepository
{
    private readonly ChatAIWebDbContext _context;

    public RagasBenchmarkResultRepository(ChatAIWebDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RagasBenchmarkResult>> GetByEvaluationQuestionIdsAsync(
        IEnumerable<int> evaluationQuestionIds,
        CancellationToken cancellationToken = default)
    {
        var ids = evaluationQuestionIds.ToList();

        return await _context.RagasBenchmarkResults
            .AsNoTracking()
            .Where(r => ids.Contains(r.EvaluationQuestionId))
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RagasBenchmarkResult
            {
                Id = r.Id,
                EvaluationQuestionId = r.EvaluationQuestionId,
                RunId = string.Empty,
                EmbeddingModel = r.EmbeddingModel,
                LlmModel = r.LlmModel,
                VectorStore = null,
                ChunkingStrategy = r.ChunkingStrategy,
                GeneratedAnswer = r.GeneratedAnswer,
                RetrievedContextsJson = null,
                Faithfulness = r.Faithfulness,
                AnswerRelevancy = r.AnswerRelevancy,
                ContextPrecision = r.ContextPrecision,
                ContextRecall = r.ContextRecall,
                OverallScore = r.OverallScore,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<RagasBenchmarkResult> results, CancellationToken cancellationToken = default)
    {
        await _context.RagasBenchmarkResults.AddRangeAsync(results, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> CountBySubjectAsync(int subjectId, CancellationToken cancellationToken = default)
    {
        return await _context.RagasBenchmarkResults
            .CountAsync(r => r.EvaluationQuestion.SubjectId == subjectId, cancellationToken);
    }

    public async Task<RagasBenchmarkResult?> GetLatestBySubjectAsync(int subjectId, CancellationToken cancellationToken = default)
    {
        return await _context.RagasBenchmarkResults
            .AsNoTracking()
            .Where(r => r.EvaluationQuestion.SubjectId == subjectId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new RagasBenchmarkResult
            {
                Id = r.Id,
                EvaluationQuestionId = r.EvaluationQuestionId,
                RunId = string.Empty,
                EmbeddingModel = r.EmbeddingModel,
                LlmModel = r.LlmModel,
                VectorStore = null,
                ChunkingStrategy = r.ChunkingStrategy,
                GeneratedAnswer = r.GeneratedAnswer,
                RetrievedContextsJson = null,
                Faithfulness = r.Faithfulness,
                AnswerRelevancy = r.AnswerRelevancy,
                ContextPrecision = r.ContextPrecision,
                ContextRecall = r.ContextRecall,
                OverallScore = r.OverallScore,
                CreatedAt = r.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RagasBenchmarkResult>> GetLatestRunBySubjectAsync(
        int subjectId,
        CancellationToken cancellationToken = default)
    {
        var hasPhase2Columns = await HasPhase2BenchmarkColumnsAsync(cancellationToken);
        var latest = hasPhase2Columns
            ? await _context.RagasBenchmarkResults
                .AsNoTracking()
                .Where(r => r.EvaluationQuestion.SubjectId == subjectId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync(cancellationToken)
            : await GetLatestBySubjectAsync(subjectId, cancellationToken);

        if (latest is null)
        {
            return [];
        }

        if (!hasPhase2Columns)
        {
            return await _context.RagasBenchmarkResults
                .AsNoTracking()
                .Where(r =>
                    r.EvaluationQuestion.SubjectId == subjectId
                    && Math.Abs(EF.Functions.DateDiffSecond(r.CreatedAt, latest.CreatedAt)) < 60)
                .OrderBy(r => r.EmbeddingModel)
                .ThenBy(r => r.EvaluationQuestionId)
                .Select(r => new RagasBenchmarkResult
                {
                    Id = r.Id,
                    EvaluationQuestionId = r.EvaluationQuestionId,
                    RunId = string.Empty,
                    EmbeddingModel = r.EmbeddingModel,
                    LlmModel = r.LlmModel,
                    VectorStore = null,
                    ChunkingStrategy = r.ChunkingStrategy,
                    GeneratedAnswer = r.GeneratedAnswer,
                    RetrievedContextsJson = null,
                    Faithfulness = r.Faithfulness,
                    AnswerRelevancy = r.AnswerRelevancy,
                    ContextPrecision = r.ContextPrecision,
                    ContextRecall = r.ContextRecall,
                    OverallScore = r.OverallScore,
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }

        return await _context.RagasBenchmarkResults
            .AsNoTracking()
            .Where(r =>
                r.EvaluationQuestion.SubjectId == subjectId
                && r.RunId == latest.RunId)
            .OrderBy(r => r.EmbeddingModel)
            .ThenBy(r => r.EvaluationQuestionId)
            .ToListAsync(cancellationToken);
    }

    private async Task<bool> HasPhase2BenchmarkColumnsAsync(CancellationToken cancellationToken)
    {
        var connection = _context.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT CASE WHEN COL_LENGTH('dbo.RagasBenchmarkResults', 'RunId') IS NULL THEN 0 ELSE 1 END";

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result) == 1;
    }
}
