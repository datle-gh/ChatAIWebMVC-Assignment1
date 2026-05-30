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
    private const int Phase4QuestionTarget = 50;

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

    public IReadOnlyList<BenchmarkChunkingStrategyDto> GetChunkingStrategies()
    {
        return
        [
            new BenchmarkChunkingStrategyDto(
                "default",
                "Default",
                "Top-K va so chunk ngu canh theo cau hinh he thong.",
                true),
            new BenchmarkChunkingStrategyDto(
                "precision",
                "Precision",
                "Lay it chunk hon de uu tien do chinh xac va giam nhieu.",
                false),
            new BenchmarkChunkingStrategyDto(
                "recall",
                "Recall",
                "Lay nhieu chunk hon de kiem tra do bao phu ngu canh.",
                false)
        ];
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

        var existingQuestions = await _questionRepository.GetBySubjectAsync(subjectId, cancellationToken);
        if (existingQuestions.Count >= Phase4QuestionTarget)
        {
            return new OperationResult(false, $"Mon hoc nay da co du {Phase4QuestionTarget} cau hoi benchmark.");
        }

        var seededAt = DateTime.UtcNow;
        var existingQuestionTexts = existingQuestions
            .Select(question => question.Question)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var phase4Questions = GeneratePhase4QuestionTemplates(subject)
            .Where(template => !existingQuestionTexts.Contains(template.Question))
            .Take(Phase4QuestionTarget - existingQuestions.Count)
            .Select((template, index) => new EvaluationQuestion
            {
                SubjectId = subjectId,
                Question = template.Question,
                GroundTruthAnswer = template.GroundTruthAnswer,
                CreatedBy = createdBy,
                CreatedAt = seededAt.AddTicks(index)
            })
            .ToList();

        if (phase4Questions.Count == 0)
        {
            return new OperationResult(false, "Khong co cau hoi mau moi de them.");
        }

        await _questionRepository.AddRangeAsync(phase4Questions, cancellationToken);
        return new OperationResult(true, $"Da tao them {phase4Questions.Count} cau hoi. Tong test set muc tieu: {Phase4QuestionTarget} cau.");
    }


    public async Task<RagasRunSummaryDto?> RunEvaluationAsync(
        int subjectId,
        IReadOnlyList<string>? embeddingModels = null,
        IReadOnlyList<string>? chunkingStrategies = null,
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
        var strategies = ResolveChunkingStrategies(chunkingStrategies);
        var runId = $"ragas-{DateTime.UtcNow:yyyyMMddHHmmss}";
        var runTime = DateTime.UtcNow;
        var results = new List<RagasBenchmarkResult>();

        foreach (var modelKey in modelKeys)
        {
            await _backfillService.BackfillSubjectAsync(subjectId, modelKey, cancellationToken);

            var embeddingService = _embeddingModelRegistry.GetRequired(modelKey);

            foreach (var strategy in strategies)
            {
                var pendingResults = new List<RagasBenchmarkResult>();
                var samples = new List<RagasEvaluationSample>();

                foreach (var question in questions)
                {
                    var questionEmbedding = await embeddingService.GenerateEmbeddingAsync(question.Question, cancellationToken);
                    var retrievedChunks = await _vectorSearchService.SearchAsync(
                        subjectId,
                        embeddingService.ModelKey,
                        questionEmbedding,
                        strategy.TopK,
                        cancellationToken);

                    var contextChunks = retrievedChunks
                        .Take(strategy.MaxContextChunks)
                        .ToList();

                    var answer = await _llmService.GenerateAnswerAsync(
                        _promptBuilder.Build(question.Question, contextChunks),
                        cancellationToken);

                    pendingResults.Add(new RagasBenchmarkResult
                    {
                        EvaluationQuestionId = question.Id,
                        RunId = runId,
                        EmbeddingModel = embeddingService.ModelKey,
                        LlmModel = _llmService.ModelName,
                        VectorStore = retrievedChunks.FirstOrDefault()?.RetrievalBackend ?? "Sql",
                        ChunkingStrategy = strategy.Key,
                        GeneratedAnswer = answer,
                        RetrievedContextsJson = JsonSerializer.Serialize(contextChunks.Select(chunk => chunk.Content).ToList()),
                        CreatedAt = runTime
                    });

                    samples.Add(new RagasEvaluationSample(
                        question.Question,
                        question.GroundTruthAnswer,
                        answer,
                        contextChunks.Select(chunk => chunk.Content).ToList()));
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

    private IReadOnlyList<BenchmarkChunkingStrategy> ResolveChunkingStrategies(IReadOnlyList<string>? requestedStrategies)
    {
        var allStrategies = GetBenchmarkChunkingStrategies();
        var requested = requestedStrategies?
            .Where(strategy => !string.IsNullOrWhiteSpace(strategy))
            .Select(strategy => strategy.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var selected = allStrategies
            .Where(strategy => requested is null || requested.Contains(strategy.Key))
            .ToList();

        return selected.Count > 0
            ? selected
            : [allStrategies.First(strategy => string.Equals(strategy.Key, "default", StringComparison.OrdinalIgnoreCase))];
    }

    private static IReadOnlyList<BenchmarkChunkingStrategy> GetBenchmarkChunkingStrategies()
    {
        return
        [
            new BenchmarkChunkingStrategy(
                "default",
                "Default",
                "Top-K va so chunk ngu canh theo cau hinh he thong.",
                TopK: 5,
                MaxContextChunks: 5),
            new BenchmarkChunkingStrategy(
                "precision",
                "Precision",
                "Lay it chunk hon de uu tien do chinh xac va giam nhieu.",
                TopK: 3,
                MaxContextChunks: 3),
            new BenchmarkChunkingStrategy(
                "recall",
                "Recall",
                "Lay nhieu chunk hon de kiem tra do bao phu ngu canh.",
                TopK: 8,
                MaxContextChunks: 8)
        ];
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
            .GroupBy(result => new { result.EmbeddingModel, result.ChunkingStrategy })
            .Select(group => new RagasModelSummaryDto(
                group.Key.EmbeddingModel,
                group.FirstOrDefault()?.LlmModel,
                group.FirstOrDefault()?.VectorStore,
                group.Key.ChunkingStrategy,
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
            topSummary?.ChunkingStrategy ?? "default",
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

    private static IReadOnlyList<Phase4QuestionTemplate> GeneratePhase4QuestionTemplates(Subject subject)
    {
        var subjectName = string.IsNullOrWhiteSpace(subject.SubjectName) ? "mon hoc" : subject.SubjectName;
        var subjectCode = string.IsNullOrWhiteSpace(subject.SubjectCode) ? "mon hoc nay" : subject.SubjectCode;

        return
        [
            Q($"Tong quan cua {subjectName} la gi?", $"Cau tra loi can tom tat dung noi dung tong quan cua {subjectName} dua tren tai lieu da index."),
            Q($"Muc tieu hoc tap chinh cua {subjectCode} la gi?", $"Cau tra loi can neu cac muc tieu hoc tap cua {subjectCode} duoc trinh bay trong tai lieu."),
            Q($"Nhung khai niem nen tang nao xuat hien trong {subjectName}?", "Cau tra loi can liet ke cac khai niem nen tang co trong tai lieu va giai thich ngan gon."),
            Q($"Tai lieu mo ta quy trinh hoac cac buoc thuc hien nao quan trong?", "Cau tra loi can trich ra dung quy trinh/cac buoc neu co trong ngu canh tai lieu."),
            Q($"Dinh nghia quan trong nhat trong phan tai lieu da hoc la gi?", "Cau tra loi can dua ra dinh nghia nam trong tai lieu va khong tu bo sung ngoai ngu canh."),
            Q($"Vi du minh hoa nao duoc tai lieu su dung de giai thich bai hoc?", "Cau tra loi can neu vi du co trong tai lieu, kem noi dung lien quan neu truy xuat duoc."),
            Q($"Cac thanh phan chinh cua chu de trong {subjectName} gom nhung gi?", "Cau tra loi can liet ke cac thanh phan/chuc nang/yeu to duoc tai lieu neu ra."),
            Q($"Tai lieu phan biet nhung khai niem nao voi nhau?", "Cau tra loi can neu diem giong va khac nhau neu tai lieu co noi dung so sanh."),
            Q($"Noi dung nao trong tai lieu la dieu kien tien quyet de hieu bai?", "Cau tra loi can xac dinh kien thuc nen tang hoac dieu kien tien quyet co trong tai lieu."),
            Q($"Tai lieu dua ra cong thuc, mo hinh hoac cau truc nao dang chu y?", "Cau tra loi can trinh bay cong thuc/mo hinh/cau truc neu duoc tai lieu cung cap."),
            Q($"Y nghia thuc tien cua chu de nay la gi?", "Cau tra loi can dua tren ung dung, loi ich, hoac y nghia duoc neu trong tai lieu."),
            Q($"Cac loi thuong gap hoac han che nao duoc tai lieu de cap?", "Cau tra loi can neu loi, han che, rui ro hoac luu y co trong tai lieu."),
            Q($"Tai lieu co neu tieu chi danh gia nao khong?", "Cau tra loi can neu tieu chi, dieu kien, muc do, hoac cach danh gia neu co trong tai lieu."),
            Q($"Cac thuat ngu tieng Anh quan trong trong {subjectCode} la gi?", "Cau tra loi can liet ke thuat ngu va giai thich theo tai lieu neu co."),
            Q($"Bai hoc nay co lien he voi chu de nao truoc do?", "Cau tra loi can neu moi lien he voi kien thuc truoc/chu de lien quan neu tai lieu de cap."),
            Q($"Phan nao trong tai lieu giai thich ve nguyen ly hoat dong?", "Cau tra loi can tom tat nguyen ly hoat dong dua tren ngu canh truy xuat."),
            Q($"Tai lieu neu uu diem nao cua phuong phap hoac cong nghe dang hoc?", "Cau tra loi can dua ra cac uu diem duoc tai lieu trinh bay."),
            Q($"Tai lieu neu nhuoc diem nao cua phuong phap hoac cong nghe dang hoc?", "Cau tra loi can dua ra cac nhuoc diem/han che duoc tai lieu trinh bay."),
            Q($"Cac buoc ap dung kien thuc trong bai vao bai tap la gi?", "Cau tra loi can neu trinh tu ap dung neu tai lieu co huong dan."),
            Q($"Dau hieu nao cho thay mot cau tra loi dung voi noi dung tai lieu?", "Cau tra loi can dua ra cac dau hieu/diem chinh co trong tai lieu."),
            Q($"Tai lieu co neu truong hop su dung nao cho chu de nay?", "Cau tra loi can neu use case/boi canh ap dung neu co."),
            Q($"Cac yeu cau dau vao va dau ra cua quy trinh trong tai lieu la gi?", "Cau tra loi can neu input/output hoac dieu kien bat dau/ket qua neu tai lieu de cap."),
            Q($"Khac biet giua ly thuyet va vi du trong tai lieu la gi?", "Cau tra loi can so sanh ly thuyet voi vi du dua tren noi dung tai lieu."),
            Q($"Tai lieu co dua ra so do, bang bieu hoac hinh minh hoa nao quan trong?", "Cau tra loi can mo ta dung noi dung cua so do/bang/hinh neu duoc trich xuat."),
            Q($"Sinh vien can ghi nho nhung diem chinh nao sau bai hoc?", "Cau tra loi can tong hop cac y chinh co trong tai lieu."),
            Q($"Cau hoi nao trong tai lieu co the dung de on tap chu de nay?", "Cau tra loi can dua ra cac diem on tap dua tren noi dung tai lieu."),
            Q($"Tai lieu co nhac den quy tac, quy uoc hoac nguyen tac nao khong?", "Cau tra loi can neu quy tac/quy uoc/nguyen tac neu co trong tai lieu."),
            Q($"Khi nao nen ap dung phuong phap duoc trinh bay trong tai lieu?", "Cau tra loi can dua tren dieu kien ap dung duoc tai lieu neu ra."),
            Q($"Khi nao khong nen ap dung phuong phap duoc trinh bay trong tai lieu?", "Cau tra loi can dua tren ngoai le/han che neu tai lieu co de cap."),
            Q($"Tai lieu co dua ra phan loai nao cho chu de nay?", "Cau tra loi can liet ke cac loai/nhom va mo ta ngan gon neu tai lieu co."),
            Q($"Moi quan he giua cac thanh phan trong chu de la gi?", "Cau tra loi can mo ta quan he/phu thuoc/luong thong tin giua cac thanh phan neu co."),
            Q($"Noi dung nao trong tai lieu co the gay nham lan cho sinh vien?", "Cau tra loi can neu diem de nham lan va cach tai lieu phan biet neu co."),
            Q($"Tai lieu co neu muc tieu, pham vi hoac gioi han cua chu de khong?", "Cau tra loi can neu muc tieu, pham vi, gioi han theo tai lieu."),
            Q($"Cac tu khoa quan trong nhat de truy van tai lieu mon {subjectCode} la gi?", "Cau tra loi can dua ra tu khoa noi bat duoc suy ra tu noi dung tai lieu."),
            Q($"Tai lieu co neu cau truc bai hoc hoac muc luc lien quan khong?", "Cau tra loi can neu cau truc/muc luc/phan chia noi dung neu co."),
            Q($"Noi dung nao trong tai lieu lien quan truc tiep den bai tap thuc hanh?", "Cau tra loi can xac dinh phan ly thuyet/huong dan co the dung cho bai tap."),
            Q($"Tai lieu co de cap den cong cu, ky thuat hoac framework nao?", "Cau tra loi can liet ke cong cu/ky thuat/framework va vai tro cua chung neu co."),
            Q($"Cac gia dinh hoac dieu kien ban dau trong tai lieu la gi?", "Cau tra loi can neu assumption/dieu kien ban dau duoc tai lieu dua ra."),
            Q($"Tai lieu co neu cach kiem tra ket qua hoac validation nao khong?", "Cau tra loi can neu cach kiem tra/doi chieu ket qua neu tai lieu co."),
            Q($"Tai lieu co dua ra vi du phan tich tung buoc khong?", "Cau tra loi can tom tat vi du tung buoc neu ngu canh tai lieu co."),
            Q($"Chu de nay co lien quan den bao mat, hieu nang hoac do tin cay khong?", "Cau tra loi can chi tra loi neu tai lieu co noi dung ve bao mat/hieu nang/do tin cay."),
            Q($"Nhung noi dung nao nen duoc trich dan khi tra loi cau hoi ve chu de nay?", "Cau tra loi can neu cac doan/chunk co lien quan va ly do chung ho tro cau tra loi."),
            Q($"Tai lieu co neu cac muc do, giai doan hoac cap bac nao khong?", "Cau tra loi can liet ke cac muc do/giai doan/cap bac neu co."),
            Q($"Cac thuat toan, quy trinh hoac workflow nao xuat hien trong tai lieu?", "Cau tra loi can neu ten va mo ta ngan gon cac thuat toan/quy trinh/workflow neu co."),
            Q($"Noi dung nao trong tai lieu giai thich nguyen nhan va ket qua?", "Cau tra loi can neu moi quan he nhan qua neu tai lieu trinh bay."),
            Q($"Tai lieu co so sanh cac lua chon hoac cach tiep can khac nhau khong?", "Cau tra loi can tom tat bang/phan so sanh neu tai lieu co."),
            Q($"Sinh vien nen tra loi ngan gon the nao neu duoc hoi ve chu de chinh?", "Cau tra loi can tao cau tra loi ngan gon nhung dung theo tai lieu."),
            Q($"Noi dung nao trong tai lieu can duoc hoc thuoc hoac nam chac?", "Cau tra loi can neu cac dinh nghia, quy tac, cong thuc, hoac diem chinh can nam."),
            Q($"Tai lieu co noi gi ve ung dung cua kien thuc trong du an hoac thuc te?", "Cau tra loi can dua tren phan ung dung/du an/thuc te duoc tai lieu neu."),
            Q($"Neu khong tim thay thong tin trong tai lieu, cau tra loi dung nen la gi?", "Cau tra loi dung can noi ro khong tim thay thong tin trong tai lieu da tai len va khong bia noi dung.")
        ];

        static Phase4QuestionTemplate Q(string question, string answer) => new(question, answer);
    }

    private sealed record Phase4QuestionTemplate(string Question, string GroundTruthAnswer);

    private sealed record BenchmarkChunkingStrategy(
        string Key,
        string DisplayName,
        string Description,
        int TopK,
        int MaxContextChunks);

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
