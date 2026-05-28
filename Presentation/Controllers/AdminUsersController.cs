using System.Security.Claims;
using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;
using BusinessLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Presentation.Models;

namespace Presentation.Controllers;

[Authorize(Roles = "Admin")]
public sealed class AdminUsersController : Controller
{
    private readonly IUserManagementService _userManagementService;

    public AdminUsersController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var users = await _userManagementService.GetUsersAsync(cancellationToken);
        return View(new AdminUserIndexViewModel
        {
            Users = users.Select(MapListItem).ToList()
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        PopulateRoles();
        return View(new AdminCreateUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        AdminCreateUserViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            PopulateRoles(model.Role);
            return View(model);
        }

        var result = await _userManagementService.CreateUserAsync(
            new CreateUserRequestDto(
                model.FullName,
                model.Email,
                model.Role,
                model.Password,
                model.IsActive),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            PopulateRoles(model.Role);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(
        int id,
        CancellationToken cancellationToken)
    {
        var user = await _userManagementService.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound("Không tìm thấy tài khoản.");
        }

        PopulateRoles(user.Role);
        return View(new AdminEditUserViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        AdminEditUserViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            PopulateRoles(model.Role);
            return View(model);
        }

        var result = await _userManagementService.UpdateUserAsync(
            new UpdateUserRequestDto(
                model.Id,
                GetCurrentUserId(),
                model.FullName,
                model.Email,
                model.Role,
                model.IsActive),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            PopulateRoles(model.Role);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> ResetPassword(
        int id,
        CancellationToken cancellationToken)
    {
        var user = await _userManagementService.GetUserAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound("Không tìm thấy tài khoản.");
        }

        return View(new AdminResetPasswordViewModel
        {
            UserId = user.Id,
            UserDisplayName = $"{user.FullName} ({user.Email})"
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(
        AdminResetPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _userManagementService.ResetPasswordAsync(
            new ResetPasswordRequestDto(model.UserId, model.NewPassword),
            cancellationToken);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["SuccessMessage"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    private void PopulateRoles(string? selectedRole = null)
    {
        ViewBag.Roles = UserRoleNames.All
            .Select(role => new SelectListItem(role, role, role == selectedRole))
            .ToList();
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : 0;
    }

    private static AdminUserListItemViewModel MapListItem(UserManagementDto user)
    {
        return new AdminUserListItemViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}
