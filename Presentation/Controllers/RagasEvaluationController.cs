using System.Security.Claims;
using BusinessLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Controllers;

[Authorize(Roles = "Admin")]
public sealed class RagasEvaluationController : Controller
{
    private readonly IRagasEvaluationService _evaluationService;
    private readonly IEmbeddingModelRegistry _embeddingModelRegistry;

    public RagasEvaluationController(
        IRagasEvaluationService evaluationService,
        IEmbeddingModelRegistry embeddingModelRegistry)
    {
        _evaluationService = evaluationService;
        _embeddingModelRegistry = embeddingModelRegistry;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var summaries = await _evaluationService.GetSubjectSummariesAsync(cancellationToken);
        var model = new RagasSubjectListViewModel
        {
            Subjects = summaries.Select(summary => new RagasSubjectItem
            {
                SubjectId = summary.SubjectId,
                SubjectCode = summary.SubjectCode,
                SubjectName = summary.SubjectName,
                QuestionCount = summary.QuestionCount,
                BenchmarkRunCount = summary.BenchmarkRunCount,
                LastOverallScore = summary.LastOverallScore,
                LastRunDate = summary.LastRunDate
            }).ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Questions(int subjectId, CancellationToken cancellationToken)
    {
        var questions = await _evaluationService.GetQuestionsAsync(subjectId, cancellationToken);
        var model = new RagasQuestionsViewModel
        {
            SubjectId = subjectId,
            SubjectName = questions.FirstOrDefault()?.SubjectName ?? $"Mon hoc {subjectId}",
            EmbeddingModels = _embeddingModelRegistry.GetAvailableModels(benchmarkOnly: true)
                .Select(embeddingModel => new RagasEmbeddingModelOption
                {
                    Key = embeddingModel.Key,
                    Provider = embeddingModel.Provider,
                    Model = embeddingModel.Model,
                    IsSelected = embeddingModel.Enabled
                })
                .ToList(),
            Questions = questions.Select(question => new RagasQuestionItem
            {
                Id = question.Id,
                Question = question.Question,
                GroundTruthAnswer = question.GroundTruthAnswer,
                CreatedByName = question.CreatedByName,
                CreatedAt = question.CreatedAt
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddQuestion(RagasAddQuestionViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _evaluationService.AddQuestionAsync(
            model.SubjectId,
            model.Question,
            model.GroundTruthAnswer,
            userId,
            cancellationToken);

        return RedirectToAction("Questions", new { subjectId = model.SubjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteQuestion(int id, int subjectId, CancellationToken cancellationToken)
    {
        await _evaluationService.DeleteQuestionAsync(id, cancellationToken);
        return RedirectToAction("Questions", new { subjectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SeedQuestions(int subjectId, CancellationToken cancellationToken)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _evaluationService.SeedQuestionsAsync(subjectId, userId, cancellationToken);
        return RedirectToAction("Questions", new { subjectId });
    }

    [HttpPost]
    public async Task<IActionResult> RunEvaluation(
        [FromBody] RunEvaluationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _evaluationService.RunEvaluationAsync(
                request.SubjectId,
                request.EmbeddingModels,
                cancellationToken);

            if (result is null)
            {
                return Json(new
                {
                    success = false,
                    message = "Khong the chay danh gia. Vui long kiem tra cau hoi benchmark."
                });
            }

            return Json(new { success = true, subjectId = request.SubjectId });
        }
        catch (Exception exception)
        {
            return Json(new { success = false, message = exception.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int subjectId, CancellationToken cancellationToken)
    {
        var result = await _evaluationService.GetLatestRunAsync(subjectId, cancellationToken);
        if (result is null)
        {
            return RedirectToAction("Index");
        }

        var model = new RagasRunResultViewModel
        {
            SubjectId = result.SubjectId,
            SubjectName = result.SubjectName,
            EmbeddingModel = result.EmbeddingModel,
            LlmModel = result.LlmModel,
            ChunkingStrategy = result.ChunkingStrategy,
            QuestionCount = result.QuestionCount,
            AvgFaithfulness = result.AvgFaithfulness,
            AvgAnswerRelevancy = result.AvgAnswerRelevancy,
            AvgContextPrecision = result.AvgContextPrecision,
            AvgContextRecall = result.AvgContextRecall,
            AvgOverallScore = result.AvgOverallScore,
            RunDate = result.RunDate,
            ModelSummaries = result.ModelSummaries.Select(summary => new RagasModelSummaryItem
            {
                EmbeddingModel = summary.EmbeddingModel,
                LlmModel = summary.LlmModel,
                VectorStore = summary.VectorStore,
                QuestionCount = summary.QuestionCount,
                AvgFaithfulness = summary.AvgFaithfulness,
                AvgAnswerRelevancy = summary.AvgAnswerRelevancy,
                AvgContextPrecision = summary.AvgContextPrecision,
                AvgContextRecall = summary.AvgContextRecall,
                AvgOverallScore = summary.AvgOverallScore
            }).ToList(),
            Results = result.Results.Select(item => new RagasResultDetailItem
            {
                EmbeddingModel = item.EmbeddingModel,
                Question = item.Question,
                GroundTruthAnswer = item.GroundTruthAnswer,
                GeneratedAnswer = item.GeneratedAnswer,
                Faithfulness = item.Faithfulness,
                AnswerRelevancy = item.AnswerRelevancy,
                ContextPrecision = item.ContextPrecision,
                ContextRecall = item.ContextRecall,
                OverallScore = item.OverallScore
            }).ToList()
        };

        return View(model);
    }
}

public sealed class RunEvaluationRequest
{
    public int SubjectId { get; set; }

    public List<string> EmbeddingModels { get; set; } = new();
}
