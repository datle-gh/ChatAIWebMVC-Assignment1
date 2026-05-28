using BusinessLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminDashboardController : Controller
{
    private readonly IAdminDashboardService _dashboardService;

    public AdminDashboardController(IAdminDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var data = await _dashboardService.GetDashboardAsync(cancellationToken);
        var model = new AdminDashboardViewModel
        {
            StudentCount = data.StudentCount,
            TeacherCount = data.TeacherCount,
            AdminCount = data.AdminCount,
            SubjectCount = data.SubjectCount,
            TotalDocumentCount = data.TotalDocumentCount,
            IndexedDocumentCount = data.IndexedDocumentCount,
            ProcessingDocumentCount = data.ProcessingDocumentCount,
            FailedDocumentCount = data.FailedDocumentCount,
            PdfCount = data.PdfCount,
            DocxCount = data.DocxCount,
            PptxCount = data.PptxCount,
            EvaluationQuestionCount = data.EvaluationQuestionCount,
            BenchmarkRunCount = data.BenchmarkRunCount,
            ChatSessionCount = data.ChatSessionCount,
            ChatMessageCount = data.ChatMessageCount,
            RecentDocuments = data.RecentDocuments.Select(d => new RecentDocumentItem
            {
                Id = d.Id, Title = d.Title, FileType = d.FileType,
                SubjectName = d.SubjectName, Status = d.Status, UploadedAt = d.UploadedAt
            }).ToList(),
            RecentBenchmarks = data.RecentBenchmarks.Select(b => new RecentBenchmarkItem
            {
                Id = b.Id, SubjectName = b.SubjectName, EmbeddingModel = b.EmbeddingModel,
                LlmModel = b.LlmModel, OverallScore = b.OverallScore, CreatedAt = b.CreatedAt
            }).ToList()
        };
        return View(model);
    }
}
