using System.Security.Claims;
using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;
using BusinessLogic.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Controllers;

[Authorize]
public sealed class ChatController : Controller
{
    private readonly IChatbotService _chatbotService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly IDocumentService _documentService;

    public ChatController(
        IChatbotService chatbotService,
        IChatHistoryService chatHistoryService,
        IDocumentService documentService)
    {
        _chatbotService = chatbotService;
        _chatHistoryService = chatHistoryService;
        _documentService = documentService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        int? subjectId,
        int? chatSessionId,
        CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var subjects = await GetSubjectsAsync(cancellationToken);
        var model = new ChatPageViewModel
        {
            ChatSessionId = chatSessionId,
            Subjects = subjects,
            Ask = new ChatAskViewModel
            {
                SubjectId = subjectId ?? 0,
                ChatSessionId = chatSessionId
            },
            RecentSessions = MapSessionItems(
                await _chatHistoryService.GetSessionsAsync(userId, subjectId, cancellationToken))
        };

        if (chatSessionId.HasValue)
        {
            var history = await _chatHistoryService.GetHistoryAsync(
                chatSessionId.Value,
                userId,
                cancellationToken);

            if (history is null)
            {
                ModelState.AddModelError(string.Empty, "Không tìm thấy phiên hội thoại hoặc bạn không có quyền truy cập.");
            }
            else
            {
                model.Ask.SubjectId = history.Session.SubjectId ?? subjectId ?? 0;
                model.Messages = MapMessages(history.Messages);
            }
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        ChatPageViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            model.Subjects = await GetSubjectsAsync(cancellationToken);
            model.RecentSessions = MapSessionItems(
                await _chatHistoryService.GetSessionsAsync(GetCurrentUserId(), model.Ask.SubjectId, cancellationToken));
            return View(model);
        }

        var response = await AskCoreAsync(model.Ask, cancellationToken);
        if (response.Succeeded && response.ChatSessionId.HasValue)
        {
            return RedirectToAction(nameof(History), new { sessionId = response.ChatSessionId.Value });
        }

        ModelState.AddModelError(string.Empty, response.ErrorMessage ?? response.Answer);
        model.Subjects = await GetSubjectsAsync(cancellationToken);
        model.RecentSessions = MapSessionItems(
            await _chatHistoryService.GetSessionsAsync(GetCurrentUserId(), model.Ask.SubjectId, cancellationToken));
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Ask(
        ChatPageViewModel model,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                succeeded = false,
                errorMessage = "Vui lòng kiểm tra lại thông tin câu hỏi."
            });
        }

        var response = await AskCoreAsync(model.Ask, cancellationToken);
        if (!response.Succeeded)
        {
            return BadRequest(response);
        }

        return Json(response);
    }

    [HttpGet]
    public async Task<IActionResult> Sessions(
        int? subjectId,
        CancellationToken cancellationToken)
    {
        var sessions = await _chatHistoryService.GetSessionsAsync(
            GetCurrentUserId(),
            subjectId,
            cancellationToken);

        return View(new ChatSessionListViewModel
        {
            SubjectId = subjectId,
            Sessions = MapSessionItems(sessions)
        });
    }

    [HttpGet]
    public async Task<IActionResult> History(
        int sessionId,
        CancellationToken cancellationToken)
    {
        var history = await _chatHistoryService.GetHistoryAsync(
            sessionId,
            GetCurrentUserId(),
            cancellationToken);

        if (history is null)
        {
            return NotFound("Không tìm thấy phiên hội thoại hoặc bạn không có quyền truy cập.");
        }

        return View(new ChatHistoryViewModel
        {
            Session = MapSessionItem(history.Session),
            Messages = MapMessages(history.Messages)
        });
    }

    private async Task<ChatResponseDto> AskCoreAsync(
        ChatAskViewModel model,
        CancellationToken cancellationToken)
    {
        return await _chatbotService.AskAsync(
            new ChatRequestDto(
                model.SubjectId,
                model.Question,
                model.ChatSessionId,
                GetCurrentUserId()),
            cancellationToken);
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : 0;
    }

    private async Task<IReadOnlyList<SubjectOptionViewModel>> GetSubjectsAsync(CancellationToken cancellationToken)
    {
        var dtos = await _documentService.GetSubjectOptionsAsync(cancellationToken);
        return dtos.Select(s => new SubjectOptionViewModel
        {
            Id = s.Id,
            SubjectCode = s.SubjectCode,
            SubjectName = s.SubjectName
        }).ToList();
    }

    private static IReadOnlyList<ChatSessionListItemViewModel> MapSessionItems(
        IReadOnlyList<ChatSessionSummaryDto> sessions)
    {
        return sessions.Select(MapSessionItem).ToList();
    }

    private static ChatSessionListItemViewModel MapSessionItem(ChatSessionSummaryDto session)
    {
        return new ChatSessionListItemViewModel
        {
            Id = session.Id,
            SubjectId = session.SubjectId,
            SubjectName = session.SubjectName,
            Title = session.Title,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            MessageCount = session.MessageCount
        };
    }

    private static IReadOnlyList<ChatMessageViewModel> MapMessages(
        IReadOnlyList<ChatMessageDto> messages)
    {
        return messages
            .Select(message => new ChatMessageViewModel
            {
                Id = message.Id,
                Role = message.Role,
                Content = message.Content,
                ModelName = message.ModelName,
                CreatedAt = message.CreatedAt,
                Citations = MapCitations(message.Citations)
            })
            .ToList();
    }

    private static IReadOnlyList<CitationViewModel> MapCitations(
        IReadOnlyList<CitationResponseDto> citations)
    {
        return citations
            .Select(citation => new CitationViewModel
            {
                CitationIndex = citation.CitationIndex,
                DocumentTitle = citation.DocumentTitle,
                PageNumber = citation.PageNumber,
                SlideNumber = citation.SlideNumber,
                ChunkIndex = citation.ChunkIndex,
                SimilarityScore = citation.SimilarityScore,
                Snippet = citation.Snippet
            })
            .ToList();
    }
}
