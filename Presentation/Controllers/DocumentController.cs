using System.Security.Claims;
using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;
using BusinessLogic.Infrastructure;
using BusinessLogic.Services;
using BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Models;

namespace Presentation.Controllers;

[Authorize]
public sealed class DocumentController : Controller
{
    private readonly IDocumentService _documentService;
    private readonly UploadSettings _uploadSettings;

    public DocumentController(
        IDocumentService documentService,
        UploadSettings uploadSettings)
    {
        _documentService = documentService;
        _uploadSettings = uploadSettings;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        string? searchTerm,
        int? subjectId,
        DocumentStatus? status,
        CancellationToken cancellationToken)
    {
        var result = await _documentService.GetDocumentsAsync(
            new DocumentListRequestDto(searchTerm, subjectId, status),
            cancellationToken);

        return View(new DocumentIndexViewModel
        {
            SearchTerm = searchTerm,
            SubjectId = subjectId,
            Status = status,
            Subjects = MapSubjects(result.Subjects),
            Documents = result.Documents.Select(MapDocumentListItem).ToList()
        });
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Teacher")]
    public async Task<IActionResult> Upload(CancellationToken cancellationToken)
    {
        return View(new DocumentUploadViewModel
        {
            UploadId = Guid.NewGuid().ToString("N"),
            MaxFileSizeMb = _uploadSettings.MaxFileSizeMb,
            MaxFilesPerBatch = _uploadSettings.MaxFilesPerBatch,
            MaxBatchSizeMb = _uploadSettings.MaxBatchSizeMb,
            Subjects = await GetSubjectOptionsAsync(cancellationToken)
        });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Teacher")]
    [ValidateAntiForgeryToken]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(
        DocumentUploadViewModel model,
        CancellationToken cancellationToken)
    {
        if (model.Files.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Files), "Vui lòng chọn tài liệu để tải lên.");
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(new
            {
                succeeded = false,
                message = GetFirstModelError()
            });
        }

        var fileRequests = new List<DocumentBatchUploadFileRequest>();
        var streams = new List<Stream>();

        try
        {
            foreach (var file in model.Files)
            {
                var stream = file.OpenReadStream();
                streams.Add(stream);
                fileRequests.Add(new DocumentBatchUploadFileRequest(
                    stream,
                    file.FileName,
                    file.ContentType,
                    file.Length));
            }

            var result = await _documentService.UploadBatchAndIndexAsync(
                new DocumentBatchUploadRequest(
                    string.IsNullOrWhiteSpace(model.UploadId) ? Guid.NewGuid().ToString("N") : model.UploadId,
                    fileRequests,
                    model.SubjectId.GetValueOrDefault(),
                    GetCurrentUserId(),
                    User.FindFirstValue(ClaimTypes.Role),
                    model.Title),
                cancellationToken);

            return Json(new
            {
                succeeded = result.Succeeded,
                message = result.Message,
                items = result.Items.Select(item => new
                {
                    succeeded = item.Succeeded,
                    documentId = item.DocumentId,
                    fileName = item.FileName,
                    message = item.Message
                })
            });
        }
        finally
        {
            foreach (var stream in streams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    private int GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(value, out var userId) ? userId : 0;
    }

    private string GetFirstModelError()
    {
        return ModelState.Values
            .SelectMany(entry => entry.Errors)
            .Select(error => error.ErrorMessage)
            .FirstOrDefault(message => !string.IsNullOrWhiteSpace(message))
            ?? "Dữ liệu tải lên không hợp lệ.";
    }

    private async Task<IReadOnlyList<SubjectOptionViewModel>> GetSubjectOptionsAsync(
        CancellationToken cancellationToken)
    {
        var subjects = await _documentService.GetUploadSubjectOptionsAsync(
            GetCurrentUserId(),
            User.FindFirstValue(ClaimTypes.Role),
            cancellationToken);
        return MapSubjects(subjects);
    }

    private static IReadOnlyList<SubjectOptionViewModel> MapSubjects(
        IReadOnlyList<SubjectOptionDto> subjects)
    {
        return subjects
            .Select(subject => new SubjectOptionViewModel
            {
                Id = subject.Id,
                SubjectCode = subject.SubjectCode,
                SubjectName = subject.SubjectName
            })
            .ToList();
    }

    private static DocumentListItemViewModel MapDocumentListItem(DocumentListItemDto document)
    {
        return new DocumentListItemViewModel
        {
            Id = document.Id,
            SubjectId = document.SubjectId,
            SubjectName = document.SubjectName,
            Title = document.Title,
            OriginalFileName = document.OriginalFileName,
            FileType = document.FileType,
            FileSizeBytes = document.FileSizeBytes,
            UploadedByName = document.UploadedByName,
            Status = document.Status,
            ErrorMessage = document.ErrorMessage,
            UploadedAt = document.UploadedAt,
            IndexedAt = document.IndexedAt,
            ChunkCount = document.ChunkCount,
            TotalTokenCount = document.TotalTokenCount,
            EmbeddingModel = document.EmbeddingModel,
            PreviewChunkIndex = document.PreviewChunkIndex,
            PreviewContent = document.PreviewContent
        };
    }
}
