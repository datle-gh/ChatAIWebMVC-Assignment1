using System.Security.Claims;
using BusinessLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Controllers;

[Authorize]
public sealed class DashboardController : Controller
{
    private readonly ISubjectService _subjectService;

    public DashboardController(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (User.IsInRole("Admin"))
        {
            return RedirectToAction("Index", "AdminDashboard");
        }

        var userId = GetCurrentUserId();
        var dashboard = await _subjectService.GetStudentDashboardAsync(userId, cancellationToken);

        var model = new DashboardViewModel
        {
            UserName = User.Identity?.Name ?? "Sinh viên",
            SubjectCount = dashboard.SubjectCount,
            ChatSessionCount = dashboard.ChatSessionCount,
            IndexedDocumentCount = dashboard.IndexedDocumentCount,
            RecentCourses = dashboard.RecentCourses
                .Select(c => new RecentCourseViewModel
                {
                    Id = c.Id,
                    SubjectCode = c.SubjectCode,
                    SubjectName = c.SubjectName,
                    Description = c.Description,
                    DocumentCount = c.DocumentCount,
                    IndexedDocumentCount = c.IndexedDocumentCount,
                    ChatSessionCount = c.ChatSessionCount,
                    ProgressPercent = c.ProgressPercent
                })
                .ToList()
        };

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> SelectSubject(CancellationToken cancellationToken)
    {
        var subjects = await _subjectService.GetAllSubjectsAsync(cancellationToken);

        var model = new SelectSubjectViewModel
        {
            Subjects = subjects
                .Select(s => new SubjectSelectionItemViewModel
                {
                    Id = s.Id,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName,
                    Description = s.Description,
                    IndexedDocumentCount = s.IndexedDocumentCount
                })
                .ToList()
        };

        return View(model);
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : 0;
    }
}
