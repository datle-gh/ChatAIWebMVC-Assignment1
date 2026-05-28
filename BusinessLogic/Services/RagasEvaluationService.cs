using System.Text.Json;
using System.Text.Json.Serialization;
using BusinessLogic.DTOs.Responses;
using BusinessLogic.Infrastructure;
using BusinessObject.Entities;
using DataAccess.Repositories;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services;

public sealed class RagasEvaluationService : IRagasEvaluationService
{
    private readonly ISubjectRepository _subjectRepository;
    private readonly IEvaluationQuestionRepository _questionRepository;
    private readonly IRagasBenchmarkResultRepository _resultRepository;
    private readonly IEmbeddingModelRegistry _embeddingModelRegistry;
    private readonly IEmbeddingBackfillService _backfillService;
    private readonly IVectorSearchService _vectorSearchService;
    private readonly IRagasEvaluatorClient _ragasEvaluatorClient;
    private readonly ILlmService _llmService;
    private readonly PromptBuilder _promptBuilder;
    private readonly ISystemSettingsService _settingsService;
    private readonly ILogger<RagasEvaluationService> _logger;

    public RagasEvaluationService(
        ISubjectRepository subjectRepository,
        IEvaluationQuestionRepository questionRepository,
        IRagasBenchmarkResultRepository resultRepository,
        IEmbeddingModelRegistry embeddingModelRegistry,
        IEmbeddingBackfillService backfillService,
        IVectorSearchService vectorSearchService,
        IRagasEvaluatorClient ragasEvaluatorClient,
        ILlmService llmService,
        PromptBuilder promptBuilder,
        ISystemSettingsService settingsService,
        ILogger<RagasEvaluationService> logger)
    {
        _subjectRepository = subjectRepository;
        _questionRepository = questionRepository;
        _resultRepository = resultRepository;
        _embeddingModelRegistry = embeddingModelRegistry;
        _backfillService = backfillService;
        _vectorSearchService = vectorSearchService;
        _ragasEvaluatorClient = ragasEvaluatorClient;
        _llmService = llmService;
        _promptBuilder = promptBuilder;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SubjectEvaluationSummaryDto>> GetSubjectSummariesAsync(CancellationToken cancellationToken = default)
    {
        var subjects = await _subjectRepository.GetAllAsync(cancellationToken);
        var summaries = new List<SubjectEvaluationSummaryDto>();

        foreach (var subject in subjects)
        {
            var questionCount = await _questionRepository.CountBySubjectAsync(subject.Id, cancellationToken);
            var runCount = await _resultRepository.CountBySubjectAsync(subject.Id, cancellationToken);
            var latestRun = await _resultRepository.GetLatestBySubjectAsync(subject.Id, cancellationToken);

            summaries.Add(new SubjectEvaluationSummaryDto(
                subject.Id,
                subject.SubjectCode,
                subject.SubjectName,
                questionCount,
                runCount,
                latestRun?.OverallScore,
                latestRun?.CreatedAt));
        }

        return summaries;
    }

    public async Task<IReadOnlyList<EvaluationQuestionDto>> GetQuestionsAsync(int subjectId, CancellationToken cancellationToken = default)
    {
        var questions = await _questionRepository.GetBySubjectAsync(subjectId, cancellationToken);
        return questions.Select(q => new EvaluationQuestionDto(
            q.Id,
            q.SubjectId,
            q.Subject?.SubjectName ?? string.Empty,
            q.Question,
            q.GroundTruthAnswer,
            q.CreatedByNavigation?.FullName,
            q.CreatedAt)).ToList();
    }

    public async Task<OperationResult> AddQuestionAsync(int subjectId, string question, string groundTruthAnswer, int createdBy, CancellationToken cancellationToken = default)
    {
        try
        {
            await _questionRepository.AddAsync(new EvaluationQuestion
            {
                SubjectId = subjectId,
                Question = question.Trim(),
                GroundTruthAnswer = groundTruthAnswer.Trim(),
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            }, cancellationToken);

            return new OperationResult(true, "Thêm câu hỏi thành công.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to add evaluation question");
            return new OperationResult(false, "Đã xảy ra lỗi hệ thống.");
        }
    }

    public async Task<OperationResult> UpdateQuestionAsync(int id, string question, string groundTruthAnswer, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _questionRepository.GetByIdAsync(id, cancellationToken);
            if (entity is null)
            {
                return new OperationResult(false, "Không tìm thấy câu hỏi.");
            }

            entity.Question = question.Trim();
            entity.GroundTruthAnswer = groundTruthAnswer.Trim();
            await _questionRepository.UpdateAsync(entity, cancellationToken);
            return new OperationResult(true, "Cập nhật câu hỏi thành công.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to update evaluation question");
            return new OperationResult(false, "Đã xảy ra lỗi hệ thống.");
        }
    }

    public async Task<OperationResult> DeleteQuestionAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var entity = await _questionRepository.GetByIdAsync(id, cancellationToken);
            if (entity is null)
            {
                return new OperationResult(false, "Không tìm thấy câu hỏi.");
            }

            await _questionRepository.DeleteAsync(entity, cancellationToken);
            return new OperationResult(true, "Xóa câu hỏi thành công.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to delete evaluation question");
            return new OperationResult(false, "Đã xảy ra lỗi hệ thống.");
        }
    }

    public async Task<OperationResult> SeedQuestionsAsync(int subjectId, int createdBy, CancellationToken cancellationToken = default)
    {
        var subject = await _subjectRepository.GetByIdAsync(subjectId, cancellationToken);
        if (subject is null)
        {
            return new OperationResult(false, "Không tìm thấy môn học.");
        }

        var currentCount = await _questionRepository.CountBySubjectAsync(subjectId, cancellationToken);
        if (currentCount > 0)
        {
            return new OperationResult(false, "Môn học này đã có câu hỏi, không thể tạo dữ liệu mẫu.");
        }

        var now = DateTime.UtcNow;
        var questions = new List<EvaluationQuestion>
        {
            new()
            {
                SubjectId = subjectId,
                Question = "Tài liệu cơ bản của môn học này là gì?",
                GroundTruthAnswer = "Tài liệu cơ bản dựa trên các slide bài giảng và tài liệu đã được tải lên.",
                CreatedBy = createdBy,
                CreatedAt = now
            },
            new()
            {
                SubjectId = subjectId,
                Question = "Mục tiêu chính của môn học này là gì?",
                GroundTruthAnswer = "Môn học giúp sinh viên nắm vững các khái niệm nền tảng và vận dụng vào thực tế.",
                CreatedBy = createdBy,
                CreatedAt = now
            }
        };

        await _questionRepository.AddRangeAsync(questions, cancellationToken);
        return new OperationResult(true, $"Đã tạo {questions.Count} câu hỏi mẫu.");
    }

    public async Task<RagasRunSummaryDto?> RunEvaluationAsync(
        int subjectId,
        IReadOnlyList<string>? embeddingModels = null,
        CancellationToken cancellationToken = default)
    {
        var questions = await _questionRepository.GetBySubjectAsync(subjectId, cancellationToken);
        if (questions.Count == 0)
        {
            return null;
        }

        var subject = await _subjectRepository.GetByIdAsync(subjectId, cancellationToken);
        if (subject is null)
        {
            return null;
        }

        var settings = await _settingsService.GetSettingsAsync(cancellationToken);
        var modelKeys = ResolveModelKeys(embeddingModels);
        var runId = $"ragas-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var runTime = DateTime.UtcNow;
        var results = new List<RagasBenchmarkResult>();

        foreach (var modelKey in modelKeys)
        {
            await _backfillService.BackfillSubjectAsync(subjectId, modelKey, cancellationToken);

            var embeddingService = _embeddingModelRegistry.GetRequired(modelKey);
            var pendingResults = new List<RagasBenchmarkResult>();
            var samples = new List<RagasEvaluationSample>();

            foreach (var question in questions)
            {
                var questionEmbedding = await embeddingService.GenerateEmbeddingAsync(question.Question, cancellationToken);
                var retrievedChunks = await _vectorSearchService.SearchAsync(
                    subjectId,
                    embeddingService.ModelKey,
                    questionEmbedding,
                    settings.TopK,
                    cancellationToken);

                var answer = await _llmService.GenerateAnswerAsync(
                    _promptBuilder.Build(question.Question, retrievedChunks),
                    cancellationToken);

                pendingResults.Add(new RagasBenchmarkResult
                {
                    EvaluationQuestionId = question.Id,
                    RunId = runId,
                    EmbeddingModel = embeddingService.ModelKey,
                    LlmModel = _llmService.ModelName,
                    VectorStore = retrievedChunks.FirstOrDefault()?.RetrievalBackend ?? "Sql",
                    ChunkingStrategy = "Default",
                    GeneratedAnswer = answer,
                    RetrievedContextsJson = JsonSerializer.Serialize(retrievedChunks.Select(chunk => chunk.Content).ToList()),
                    CreatedAt = runTime
                });

                samples.Add(new RagasEvaluationSample(
                    question.Question,
                    question.GroundTruthAnswer,
                    answer,
                    retrievedChunks.Select(chunk => chunk.Content).ToList()));
            }

            var scores = await EvaluateWithRagasOrFallbackAsync(samples, settings.EvaluationSystemPrompt, cancellationToken);
            for (var index = 0; index < pendingResults.Count; index++)
            {
                var score = scores[index];
                pendingResults[index].Faithfulness = score.Faithfulness;
                pendingResults[index].AnswerRelevancy = score.AnswerRelevancy;
                pendingResults[index].ContextPrecision = score.ContextPrecision;
                pendingResults[index].ContextRecall = score.ContextRecall;
                pendingResults[index].OverallScore = score.OverallScore;
            }

            results.AddRange(pendingResults);
        }

        await _resultRepository.AddRangeAsync(results, cancellationToken);
        return CreateSummary(subjectId, subject.SubjectName, runTime, results, questions);
    }

    public async Task<RagasRunSummaryDto?> GetLatestRunAsync(int subjectId, CancellationToken cancellationToken = default)
    {
        var latestRunResults = await _resultRepository.GetLatestRunBySubjectAsync(subjectId, cancellationToken);
        if (latestRunResults.Count == 0)
        {
            return null;
        }

        var subject = await _subjectRepository.GetByIdAsync(subjectId, cancellationToken);
        var questions = await _questionRepository.GetBySubjectAsync(subjectId, cancellationToken);
        return CreateSummary(
            subjectId,
            subject?.SubjectName ?? string.Empty,
            latestRunResults.Max(result => result.CreatedAt),
            latestRunResults,
            questions);
    }

    private IReadOnlyList<string> ResolveModelKeys(IReadOnlyList<string>? requestedModels)
    {
        var available = _embeddingModelRegistry.GetAvailableModels(benchmarkOnly: true);
        var requested = requestedModels?
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Select(model => model.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var selected = available
            .Where(model => requested is null || requested.Contains(model.Key))
            .Select(model => model.Key)
            .ToList();

        return selected.Count > 0
            ? selected
            : [_embeddingModelRegistry.GetDefault().ModelKey];
    }

    private async Task<IReadOnlyList<RagasEvaluationScore>> EvaluateWithRagasOrFallbackAsync(
        IReadOnlyList<RagasEvaluationSample> samples,
        string systemPrompt,
        CancellationToken cancellationToken)
    {
        try
        {
            var ragasScores = await _ragasEvaluatorClient.EvaluateAsync(samples, cancellationToken);
            if (ragasScores.Count == samples.Count)
            {
                return ragasScores;
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "RAGAS service failed. Falling back to in-process LLM judge.");
        }

        var fallbackScores = new List<RagasEvaluationScore>();
        foreach (var sample in samples)
        {
            fallbackScores.Add(await EvaluateWithLlmJudgeAsync(sample, systemPrompt, cancellationToken));
        }

        return fallbackScores;
    }

    private async Task<RagasEvaluationScore> EvaluateWithLlmJudgeAsync(
        RagasEvaluationSample sample,
        string systemPrompt,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            systemPrompt = "Bạn là hệ thống chấm điểm RAG. Đánh giá theo thang 0.0 đến 1.0 cho JSON: {\"faithfulness\":0.0,\"answer_relevancy\":0.0,\"context_precision\":0.0,\"context_recall\":0.0}";
        }

        var context = string.Join("\n", sample.RetrievedContexts);
        var prompt = $"{systemPrompt}\n\nCâu hỏi: {sample.Question}\n\nCâu trả lời chuẩn: {sample.GroundTruthAnswer}\n\nNgữ cảnh: {context}\n\nCâu trả lời sinh ra: {sample.GeneratedAnswer}\n\nTrả về chỉ JSON:";

        try
        {
            var llmResponse = await _llmService.GenerateAnswerAsync(prompt, cancellationToken);
            var jsonStartIndex = llmResponse.IndexOf('{');
            var jsonEndIndex = llmResponse.LastIndexOf('}');
            if (jsonStartIndex >= 0 && jsonEndIndex >= jsonStartIndex)
            {
                var json = llmResponse.Substring(jsonStartIndex, jsonEndIndex - jsonStartIndex + 1);
                var score = JsonSerializer.Deserialize<LlmScore>(json);
                if (score is not null)
                {
                    return new RagasEvaluationScore(
                        score.Faithfulness,
                        score.AnswerRelevancy,
                        score.ContextPrecision,
                        score.ContextRecall);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "LLM judge fallback failed");
        }

        return new RagasEvaluationScore(0.5m, 0.5m, 0.5m, 0.5m);
    }

    private static RagasRunSummaryDto CreateSummary(
        int subjectId,
        string subjectName,
        DateTime runDate,
        IReadOnlyList<RagasBenchmarkResult> results,
        IReadOnlyList<EvaluationQuestion> questions)
    {
        var questionById = questions.ToDictionary(question => question.Id);
        var modelSummaries = results
            .GroupBy(result => result.EmbeddingModel)
            .Select(group => new RagasModelSummaryDto(
                group.Key,
                group.FirstOrDefault()?.LlmModel,
                group.FirstOrDefault()?.VectorStore,
                group.Count(),
                group.Average(result => result.Faithfulness ?? 0),
                group.Average(result => result.AnswerRelevancy ?? 0),
                group.Average(result => result.ContextPrecision ?? 0),
                group.Average(result => result.ContextRecall ?? 0),
                group.Average(result => result.OverallScore ?? 0)))
            .OrderByDescending(summary => summary.AvgOverallScore)
            .ToList();

        var resultDtos = results.Select(result => new RagasBenchmarkResultDto(
            result.Id,
            result.EvaluationQuestionId,
            result.RunId,
            questionById.GetValueOrDefault(result.EvaluationQuestionId)?.Question ?? string.Empty,
            questionById.GetValueOrDefault(result.EvaluationQuestionId)?.GroundTruthAnswer,
            result.EmbeddingModel,
            result.LlmModel,
            result.VectorStore,
            result.ChunkingStrategy,
            result.GeneratedAnswer,
            result.RetrievedContextsJson,
            result.Faithfulness,
            result.AnswerRelevancy,
            result.ContextPrecision,
            result.ContextRecall,
            result.OverallScore,
            result.CreatedAt)).ToList();

        var topSummary = modelSummaries.FirstOrDefault();

        return new RagasRunSummaryDto(
            subjectId,
            subjectName,
            topSummary?.EmbeddingModel ?? string.Empty,
            topSummary?.LlmModel,
            "Default",
            questions.Count,
            modelSummaries.Count == 0 ? 0 : modelSummaries.Average(summary => summary.AvgFaithfulness),
            modelSummaries.Count == 0 ? 0 : modelSummaries.Average(summary => summary.AvgAnswerRelevancy),
            modelSummaries.Count == 0 ? 0 : modelSummaries.Average(summary => summary.AvgContextPrecision),
            modelSummaries.Count == 0 ? 0 : modelSummaries.Average(summary => summary.AvgContextRecall),
            modelSummaries.Count == 0 ? 0 : modelSummaries.Average(summary => summary.AvgOverallScore),
            runDate,
            modelSummaries,
            resultDtos);
    }

    private sealed class LlmScore
    {
        [JsonPropertyName("faithfulness")]
        public decimal Faithfulness { get; set; } = 0.5m;

        [JsonPropertyName("answer_relevancy")]
        public decimal AnswerRelevancy { get; set; } = 0.5m;

        [JsonPropertyName("context_precision")]
        public decimal ContextPrecision { get; set; } = 0.5m;

        [JsonPropertyName("context_recall")]
        public decimal ContextRecall { get; set; } = 0.5m;
    }
}
