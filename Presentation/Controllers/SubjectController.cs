using System.Security.Claims;
using BusinessLogic.DTOs.Requests;
using BusinessLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Controllers;

[Authorize(Roles = "Admin,Teacher")]
public sealed class SubjectController : Controller
{
    private readonly ISubjectService _subjectService;

    public SubjectController(ISubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var subjects = await _subjectService.GetAllSubjectsAsync(cancellationToken);

        var model = new SubjectPageViewModel
        {
            Subjects = subjects
                .Select(s => new SubjectViewModel
                {
                    Id = s.Id,
                    SubjectCode = s.SubjectCode,
                    SubjectName = s.SubjectName,
                    Description = s.Description,
                    DocumentCount = s.DocumentCount,
                    IndexedDocumentCount = s.IndexedDocumentCount,
                    StudentCount = s.StudentCount,
                    TeacherCount = s.TeacherCount,
                    CreatedAt = s.CreatedAt,
                    TeacherNames = s.TeacherNames
                })
                .ToList()
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateSubjectViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateSubjectViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _subjectService.CreateSubjectAsync(
            new CreateSubjectRequestDto(
                model.SubjectCode,
                model.SubjectName,
                model.Description,
                GetCurrentUserId(),
                User.FindFirstValue(ClaimTypes.Role)),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var subject = await _subjectService.GetSubjectByIdAsync(id, cancellationToken);
        if (subject is null)
        {
            return NotFound("Không tìm thấy môn học.");
        }

        return View(new EditSubjectViewModel
        {
            Id = subject.Id,
            SubjectCode = subject.SubjectCode,
            SubjectName = subject.SubjectName,
            Description = subject.Description
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        EditSubjectViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _subjectService.UpdateSubjectAsync(
            new UpdateSubjectRequestDto(
                model.Id,
                model.SubjectCode,
                model.SubjectName,
                model.Description),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _subjectService.DeleteSubjectAsync(id, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : 0;
    }
}
