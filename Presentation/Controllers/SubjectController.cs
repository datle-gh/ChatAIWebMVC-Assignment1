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
    public async Task<IActionResult> Index(string? filter, CancellationToken cancellationToken)
    {
        var currentUserRole = GetCurrentUserRole();
        var selectedFilter = ResolveSubjectFilter(filter, currentUserRole);
        var subjects = await _subjectService.GetManagementSubjectsAsync(
            GetCurrentUserId(),
            currentUserRole,
            selectedFilter,
            cancellationToken);

        var model = new SubjectPageViewModel
        {
            SelectedFilter = selectedFilter,
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
                    CreatedById = s.CreatedById,
                    CreatedByName = s.CreatedByName,
                    IsTeacherEnrolled = s.IsTeacherEnrolled,
                    CanManage = s.CanManage,
                    TeacherNames = s.TeacherNames,
                    MemberNames = s.MemberNames
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
        if (!await _subjectService.CanManageSubjectAsync(
                id,
                GetCurrentUserId(),
                GetCurrentUserRole(),
                cancellationToken))
        {
            TempData["ErrorMessage"] = "Bạn chỉ có quyền chỉnh sửa môn học do bạn tạo.";
            return RedirectToAction(nameof(Index));
        }

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
        if (!await _subjectService.CanManageSubjectAsync(
                model.Id,
                GetCurrentUserId(),
                GetCurrentUserRole(),
                cancellationToken))
        {
            TempData["ErrorMessage"] = "Bạn chỉ có quyền chỉnh sửa môn học do bạn tạo.";
            return RedirectToAction(nameof(Index));
        }

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
        if (!await _subjectService.CanManageSubjectAsync(
                id,
                GetCurrentUserId(),
                GetCurrentUserRole(),
                cancellationToken))
        {
            TempData["ErrorMessage"] = "Bạn chỉ có quyền xóa môn học do bạn tạo.";
            return RedirectToAction(nameof(Index));
        }

        var result = await _subjectService.DeleteSubjectAsync(id, cancellationToken);
        TempData[result.Succeeded ? "SuccessMessage" : "ErrorMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : 0;
    }

    private string? GetCurrentUserRole()
    {
        return User.FindFirstValue(ClaimTypes.Role);
    }

    private static string ResolveSubjectFilter(string? filter, string? currentUserRole)
    {
        if (string.Equals(filter, "created", StringComparison.OrdinalIgnoreCase))
        {
            return "created";
        }

        if (string.Equals(filter, "enrolled", StringComparison.OrdinalIgnoreCase))
        {
            return "enrolled";
        }

        if (string.Equals(filter, "all", StringComparison.OrdinalIgnoreCase))
        {
            return "all";
        }

        return string.Equals(currentUserRole, "Teacher", StringComparison.OrdinalIgnoreCase)
            ? "enrolled"
            : "all";
    }
}
