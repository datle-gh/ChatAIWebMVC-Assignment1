using BusinessLogic.DTOs.Requests;
using BusinessLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Controllers;

[Authorize(Roles = "Admin")]
public sealed class SystemSettingsController : Controller
{
    private readonly ISystemSettingsService _settingsService;

    public SystemSettingsController(ISystemSettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var settings = await _settingsService.GetSettingsAsync(cancellationToken);
        var model = new SystemSettingsViewModel
        {
            LlmProvider = settings.LlmProvider,
            GeminiApiKey = settings.GeminiApiKey,
            GeminiModel = settings.GeminiModel,
            OpenAiApiKey = settings.OpenAiApiKey,
            OpenAiModel = settings.OpenAiModel,
            EmbeddingProvider = settings.EmbeddingProvider,
            EmbeddingModel = settings.EmbeddingModel,
            TopK = settings.TopK,
            SimilarityThreshold = settings.SimilarityThreshold,
            MaxCitationSnippetLength = settings.MaxCitationSnippetLength,
            ChatSystemPrompt = settings.ChatSystemPrompt,
            EvaluationSystemPrompt = settings.EvaluationSystemPrompt
        };
        
        if (TempData["SuccessMessage"] != null)
        {
            model.SuccessMessage = TempData["SuccessMessage"]?.ToString();
        }
        
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SystemSettingsViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return View("Index", model);

        var settings = new SystemSettingsDto
        {
            LlmProvider = model.LlmProvider,
            GeminiApiKey = model.GeminiApiKey,
            GeminiModel = model.GeminiModel,
            OpenAiApiKey = model.OpenAiApiKey,
            OpenAiModel = model.OpenAiModel,
            EmbeddingProvider = model.EmbeddingProvider,
            EmbeddingModel = model.EmbeddingModel,
            TopK = model.TopK,
            SimilarityThreshold = model.SimilarityThreshold,
            MaxCitationSnippetLength = model.MaxCitationSnippetLength,
            ChatSystemPrompt = model.ChatSystemPrompt,
            EvaluationSystemPrompt = model.EvaluationSystemPrompt
        };

        await _settingsService.SaveSettingsAsync(settings, cancellationToken);
        
        TempData["SuccessMessage"] = "Lưu cấu hình hệ thống thành công!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> TestConnection([FromBody] TestConnectionRequest request, CancellationToken cancellationToken)
    {
        var result = await _settingsService.TestConnectionAsync(request.Provider, request.ApiKey, request.Model, cancellationToken);
        return Json(new { success = result.Success, message = result.Message });
    }
}

public class TestConnectionRequest
{
    public string Provider { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}
