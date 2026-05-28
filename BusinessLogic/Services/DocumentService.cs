using System.Text.Json;
using BusinessLogic.DTOs.Requests;
using BusinessLogic.DTOs.Responses;
using BusinessLogic.Infrastructure;
using BusinessObject.Entities;
using BusinessObject.Enums;
using DataAccess.Repositories;
using Microsoft.Extensions.Logging;

namespace BusinessLogic.Services;

public sealed class DocumentService : IDocumentService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".docx",
        ".pptx"
    };

    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentChunkRepository _documentChunkRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISubjectRepository _subjectRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ITextExtractionService _textExtractionService;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingModelRegistry _embeddingModelRegistry;
    private readonly IDocumentChunkEmbeddingRepository _documentChunkEmbeddingRepository;
    private readonly IVectorStoreService _vectorStoreService;
    private readonly IUploadProgressReporter _uploadProgressReporter;
    private readonly UploadSettings _uploadSettings;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(
        IDocumentRepository documentRepository,
        IDocumentChunkRepository documentChunkRepository,
        IUserRepository userRepository,
        ISubjectRepository subjectRepository,
        IFileStorageService fileStorageService,
        ITextExtractionService textExtractionService,
        IChunkingService chunkingService,
        IEmbeddingModelRegistry embeddingModelRegistry,
        IDocumentChunkEmbeddingRepository documentChunkEmbeddingRepository,
        IVectorStoreService vectorStoreService,
        IUploadProgressReporter uploadProgressReporter,
        UploadSettings uploadSettings,
        ILogger<DocumentService> logger)
    {
        _documentRepository = documentRepository;
        _documentChunkRepository = documentChunkRepository;
        _userRepository = userRepository;
        _subjectRepository = subjectRepository;
        _fileStorageService = fileStorageService;
        _textExtractionService = textExtractionService;
        _chunkingService = chunkingService;
        _embeddingModelRegistry = embeddingModelRegistry;
        _documentChunkEmbeddingRepository = documentChunkEmbeddingRepository;
        _vectorStoreService = vectorStoreService;
        _uploadProgressReporter = uploadProgressReporter;
        _uploadSettings = uploadSettings;
        _logger = logger;
    }

    public async Task<DocumentUploadResult> UploadAndIndexAsync(
        DocumentUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ValidateRequestAsync(request, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Document upload validation failed for file {FileName}", request.FileName);
            return new DocumentUploadResult(false, null, GetUserMessage(exception));
        }

        return await UploadAndIndexCoreAsync(request, progressContext: null, cancellationToken);
    }

    public async Task<DocumentBatchUploadResult> UploadBatchAndIndexAsync(
        DocumentBatchUploadRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await ValidateBatchRequestAsync(request, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Document batch upload validation failed for upload {UploadId}", request.UploadId);
            return new DocumentBatchUploadResult(false, GetUserMessage(exception), []);
        }

        var files = request.Files.ToList();
        var results = new List<DocumentUploadItemResult>();

        for (var index = 0; index < files.Count; index++)
        {
            var file = files[index];
            var progressContext = new UploadProgressContext(
                request.UploadId,
                request.UploadedBy!.Value,
                index + 1,
                files.Count);

            try
            {
                ValidateFile(file.FileStream, file.FileName, file.FileSizeBytes);
            }
            catch (Exception exception)
            {
                var validationMessage = GetUserMessage(exception);
                await ReportProgressAsync(
                    progressContext,
                    file.FileName,
                    "failed",
                    100,
                    validationMessage,
                    isFailed: true,
                    cancellationToken: cancellationToken);

                results.Add(new DocumentUploadItemResult(false, null, file.FileName, validationMessage));
                continue;
            }

            var title = files.Count == 1 ? request.Title : null;
            var itemResult = await UploadAndIndexCoreAsync(
                new DocumentUploadRequest(
                    file.FileStream,
                    file.FileName,
                    file.ContentType,
                    file.FileSizeBytes,
                    request.SubjectId,
                    request.UploadedBy,
                    request.UploaderRole,
                    title),
                progressContext,
                cancellationToken);

            results.Add(new DocumentUploadItemResult(
                itemResult.Succeeded,
                itemResult.DocumentId,
                file.FileName,
                itemResult.Message));
        }

        var succeededCount = results.Count(result => result.Succeeded);
        var message = succeededCount == files.Count
            ? $"Đã tải lên và index thành công {succeededCount}/{files.Count} tài liệu."
            : $"Đã xử lý {succeededCount}/{files.Count} tài liệu. Vui lòng kiểm tra các file lỗi.";

        return new DocumentBatchUploadResult(
            results.Count > 0 && results.All(result => result.Succeeded),
            message,
            results);
    }

    public async Task<DocumentListResultDto> GetDocumentsAsync(
        DocumentListRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var documents = await _documentRepository.GetListAsync(
            request.SearchTerm,
            request.SubjectId,
            request.Status,
            cancellationToken);

        var subjects = await _subjectRepository.GetAllAsync(cancellationToken);

        return new DocumentListResultDto(
            documents.Select(MapListItem).ToList(),
            subjects
                .Select(subject => new SubjectOptionDto(
                    subject.Id,
                    subject.SubjectCode,
                    subject.SubjectName))
                .ToList());
    }

    public async Task<DocumentDetailDto?> GetDocumentDetailAsync(
        int documentId,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        return document is null ? null : MapDetail(document);
    }

    public async Task<IReadOnlyList<SubjectOptionDto>> GetSubjectOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var subjects = await _subjectRepository.GetAllAsync(cancellationToken);

        return subjects
            .Select(subject => new SubjectOptionDto(
                subject.Id,
                subject.SubjectCode,
                subject.SubjectName))
            .ToList();
    }

    public async Task<IReadOnlyList<SubjectOptionDto>> GetUploadSubjectOptionsAsync(
        int userId,
        string? userRole,
        CancellationToken cancellationToken = default)
    {
        var subjects = string.Equals(userRole, UserRoleNames.Admin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(userRole, UserRoleNames.Teacher, StringComparison.OrdinalIgnoreCase)
                ? await _subjectRepository.GetAllAsync(cancellationToken)
                : [];

        return subjects
            .Select(subject => new SubjectOptionDto(
                subject.Id,
                subject.SubjectCode,
                subject.SubjectName))
            .ToList();
    }

    private async Task<DocumentUploadResult> UploadAndIndexCoreAsync(
        DocumentUploadRequest request,
        UploadProgressContext? progressContext,
        CancellationToken cancellationToken)
    {
        Document? document = null;

        try
        {
            await ReportProgressAsync(
                progressContext,
                request.FileName,
                "saving",
                10,
                "Đang lưu file...",
                cancellationToken: cancellationToken);

            var storedFile = await _fileStorageService.SaveAsync(
                request.FileStream,
                request.FileName,
                cancellationToken);

            await ReportProgressAsync(
                progressContext,
                request.FileName,
                "document",
                25,
                "Đang tạo bản ghi tài liệu...",
                cancellationToken: cancellationToken);

            document = await _documentRepository.AddAsync(
                new Document
                {
                    SubjectId = request.SubjectId,
                    Title = string.IsNullOrWhiteSpace(request.Title)
                        ? Path.GetFileNameWithoutExtension(request.FileName)
                        : request.Title.Trim(),
                    OriginalFileName = storedFile.OriginalFileName,
                    StoredFileName = storedFile.StoredFileName,
                    FilePath = storedFile.FilePath,
                    FileType = storedFile.FileType,
                    FileSizeBytes = request.FileSizeBytes,
                    UploadedBy = request.UploadedBy,
                    Status = DocumentStatus.Processing,
                    UploadedAt = DateTime.UtcNow
                },
                cancellationToken);

            await ReportProgressAsync(
                progressContext,
                request.FileName,
                "extracting",
                35,
                "Đang đọc nội dung tài liệu...",
                cancellationToken: cancellationToken);

            var extractedSegments = await _textExtractionService.ExtractAsync(
                storedFile.FilePath,
                storedFile.FileType,
                cancellationToken);

            await ReportProgressAsync(
                progressContext,
                request.FileName,
                "chunking",
                45,
                "Đang chia nội dung thành chunks...",
                cancellationToken: cancellationToken);

            var chunkDrafts = _chunkingService.SplitIntoChunks(extractedSegments);
            if (chunkDrafts.Count == 0)
            {
                throw new InvalidOperationException("Không thể đọc nội dung tài liệu.");
            }

            var chunks = new List<DocumentChunk>();
            var defaultEmbeddingService = _embeddingModelRegistry.GetDefault();
            var defaultEmbeddings = new List<float[]>();
            for (var index = 0; index < chunkDrafts.Count; index++)
            {
                var draft = chunkDrafts[index];
                var embedding = await defaultEmbeddingService.GenerateEmbeddingAsync(draft.Content, cancellationToken);
                defaultEmbeddings.Add(embedding);

                chunks.Add(new DocumentChunk
                {
                    DocumentId = document.Id,
                    Document = document,
                    ChunkIndex = draft.ChunkIndex,
                    Content = draft.Content,
                    PageNumber = draft.PageNumber,
                    SlideNumber = draft.SlideNumber,
                    TokenCount = draft.TokenCount,
                    VectorId = $"doc-{document.Id}-chunk-{draft.ChunkIndex}",
                    EmbeddingModel = defaultEmbeddingService.ModelKey,
                    EmbeddingJson = JsonSerializer.Serialize(embedding),
                    CreatedAt = DateTime.UtcNow
                });

                var embeddingPercent = 55 + (int)Math.Round(((index + 1) / (double)chunkDrafts.Count) * 30);
                await ReportProgressAsync(
                    progressContext,
                    request.FileName,
                    "embedding",
                    embeddingPercent,
                    $"Đang tạo embedding ({index + 1}/{chunkDrafts.Count})...",
                    cancellationToken: cancellationToken);
            }

            await ReportProgressAsync(
                progressContext,
                request.FileName,
                "indexing",
                90,
                "Đang lưu chunks và cập nhật trạng thái...",
                cancellationToken: cancellationToken);

            await _documentChunkRepository.AddRangeAsync(chunks, cancellationToken);
            await SaveEmbeddingsAsync(
                chunks,
                defaultEmbeddings,
                defaultEmbeddingService,
                cancellationToken);

            await _documentRepository.UpdateStatusAsync(
                document.Id,
                DocumentStatus.Indexed,
                indexedAt: DateTime.UtcNow,
                cancellationToken: cancellationToken);

            await ReportProgressAsync(
                progressContext,
                request.FileName,
                "completed",
                100,
                "Tài liệu đã được index thành công.",
                isCompleted: true,
                cancellationToken: cancellationToken);

            return new DocumentUploadResult(true, document.Id, "Tài liệu đã được tải lên và index thành công.");
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Document upload/indexing failed for file {FileName}", request.FileName);

            if (document is not null)
            {
                var failureMessage = GetUserMessage(exception);
                await _documentRepository.UpdateStatusAsync(
                    document.Id,
                    DocumentStatus.Failed,
                    failureMessage,
                    cancellationToken: cancellationToken);
            }

            var message = GetUserMessage(exception);
            await ReportProgressAsync(
                progressContext,
                request.FileName,
                "failed",
                100,
                message,
                isFailed: true,
                cancellationToken: cancellationToken);

            return new DocumentUploadResult(false, document?.Id, message);
        }
    }

    private async Task SaveEmbeddingsAsync(
        IReadOnlyList<DocumentChunk> chunks,
        IReadOnlyList<float[]> embeddings,
        IEmbeddingService embeddingService,
        CancellationToken cancellationToken)
    {
        var rows = chunks
            .Zip(embeddings)
            .Select(item => new DocumentChunkEmbedding
            {
                DocumentChunkId = item.First.Id,
                EmbeddingModel = embeddingService.ModelKey,
                EmbeddingProvider = embeddingService.ProviderName,
                Dimension = item.Second.Length,
                VectorId = $"chunk-{item.First.Id}-{embeddingService.ModelKey}",
                VectorStore = _vectorStoreService.Name,
                EmbeddingJson = JsonSerializer.Serialize(item.Second),
                CreatedAt = DateTime.UtcNow
            })
            .ToList();

        await _documentChunkEmbeddingRepository.AddRangeAsync(rows, cancellationToken);
        await _vectorStoreService.UpsertAsync(
            embeddingService.ModelKey,
            embeddingService.ProviderName,
            chunks,
            embeddings,
            cancellationToken);
    }

    private async Task ValidateRequestAsync(
        DocumentUploadRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateUploadContextAsync(
            request.UploadedBy,
            request.UploaderRole,
            request.SubjectId,
            cancellationToken);

        ValidateFile(request.FileStream, request.FileName, request.FileSizeBytes);
    }

    private async Task ValidateBatchRequestAsync(
        DocumentBatchUploadRequest request,
        CancellationToken cancellationToken)
    {
        await ValidateUploadContextAsync(
            request.UploadedBy,
            request.UploaderRole,
            request.SubjectId,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(request.UploadId))
        {
            throw new InvalidOperationException("Phiên tải lên không hợp lệ.");
        }

        if (request.Files.Count == 0)
        {
            throw new InvalidOperationException("Vui lòng chọn tài liệu để tải lên.");
        }

        if (request.Files.Count > _uploadSettings.MaxFilesPerBatch)
        {
            throw new InvalidOperationException($"Mỗi lần chỉ được tải tối đa {_uploadSettings.MaxFilesPerBatch} tài liệu.");
        }

        var totalSize = request.Files.Sum(file => file.FileSizeBytes);
        if (totalSize > _uploadSettings.MaxBatchSizeBytes)
        {
            throw new InvalidOperationException($"Tổng dung lượng mỗi lần tải không được vượt quá {_uploadSettings.MaxBatchSizeMb} MB.");
        }
    }

    private async Task ValidateUploadContextAsync(
        int? uploadedBy,
        string? uploaderRole,
        int subjectId,
        CancellationToken cancellationToken)
    {
        if (uploadedBy is null || uploadedBy <= 0)
        {
            throw new InvalidOperationException("Vui lòng đăng nhập để tải tài liệu.");
        }

        if (subjectId <= 0)
        {
            throw new InvalidOperationException("Vui lòng nhập mã môn học hợp lệ.");
        }

        if (string.Equals(uploaderRole, UserRoleNames.Admin, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uploaderRole, UserRoleNames.Teacher, StringComparison.OrdinalIgnoreCase))
        {
            var subject = await _subjectRepository.GetByIdAsync(subjectId, cancellationToken);
            if (subject is null)
            {
                throw new InvalidOperationException("Vui long chon mon hoc hop le.");
            }

            return;
        }

        if (string.Equals(uploaderRole, UserRoleNames.Teacher, StringComparison.OrdinalIgnoreCase))
        {
            var canUpload = await _userRepository.IsTeacherAssignedToSubjectAsync(
                uploadedBy.Value,
                subjectId,
                cancellationToken);

            if (!canUpload)
            {
                throw new InvalidOperationException("Bạn không có quyền tải tài liệu cho môn học này.");
            }
        }
        else if (!string.Equals(uploaderRole, UserRoleNames.Admin, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Bạn không có quyền tải tài liệu.");
        }
    }

    private void ValidateFile(
        Stream fileStream,
        string fileName,
        long fileSizeBytes)
    {
        if (fileStream is null || fileSizeBytes <= 0)
        {
            throw new InvalidOperationException("Vui lòng chọn tài liệu để tải lên.");
        }

        if (fileSizeBytes > _uploadSettings.MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"Dung lượng file vượt quá giới hạn {_uploadSettings.MaxFileSizeMb} MB.");
        }

        var extension = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("File không đúng định dạng được hỗ trợ.");
        }
    }

    private async Task ReportProgressAsync(
        UploadProgressContext? context,
        string fileName,
        string stage,
        int percent,
        string message,
        bool isCompleted = false,
        bool isFailed = false,
        CancellationToken cancellationToken = default)
    {
        if (context is null)
        {
            return;
        }

        try
        {
            await _uploadProgressReporter.ReportAsync(
                new UploadProgressDto(
                    context.UploadId,
                    context.UserId,
                    fileName,
                    context.FileIndex,
                    context.TotalFiles,
                    stage,
                    percent,
                    message,
                    isCompleted,
                    isFailed),
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to publish upload progress for {FileName}", fileName);
        }
    }

    private static string GetUserMessage(Exception exception)
    {
        return exception is InvalidOperationException
            ? exception.Message
            : "Không thể đọc nội dung tài liệu.";
    }

    private static DocumentListItemDto MapListItem(Document document)
    {
        var chunks = document.DocumentChunks
            .OrderBy(chunk => chunk.ChunkIndex)
            .ToList();
        var previewChunk = chunks.FirstOrDefault(chunk => !string.IsNullOrWhiteSpace(chunk.Content));

        return new DocumentListItemDto(
            document.Id,
            document.SubjectId,
            GetSubjectDisplayName(document),
            document.Title,
            document.OriginalFileName,
            document.FileType,
            document.FileSizeBytes,
            document.UploadedByNavigation?.FullName,
            document.Status,
            document.ErrorMessage,
            document.UploadedAt,
            document.IndexedAt,
            chunks.Count,
            chunks.Sum(chunk => chunk.TokenCount),
            chunks.FirstOrDefault(chunk => !string.IsNullOrWhiteSpace(chunk.EmbeddingModel))?.EmbeddingModel,
            previewChunk?.ChunkIndex,
            CreatePreview(previewChunk?.Content));
    }

    private static DocumentDetailDto MapDetail(Document document)
    {
        var item = MapListItem(document);

        return new DocumentDetailDto(
            item.Id,
            item.SubjectId,
            item.SubjectName,
            item.Title,
            item.OriginalFileName,
            item.FileType,
            item.FileSizeBytes,
            item.UploadedByName,
            item.Status,
            item.ErrorMessage,
            item.UploadedAt,
            item.IndexedAt,
            item.ChunkCount,
            item.TotalTokenCount,
            item.EmbeddingModel,
            item.PreviewChunkIndex,
            item.PreviewContent);
    }

    private static string GetSubjectDisplayName(Document document)
    {
        return string.IsNullOrWhiteSpace(document.Subject.SubjectCode)
            ? document.Subject.SubjectName
            : $"{document.Subject.SubjectCode} - {document.Subject.SubjectName}";
    }

    private static string? CreatePreview(string? content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        var normalized = string.Join(' ', content.Split(default(string[]), StringSplitOptions.RemoveEmptyEntries));
        return normalized.Length <= 420 ? normalized : $"{normalized[..420]}...";
    }

    private sealed record UploadProgressContext(
        string UploadId,
        int UserId,
        int FileIndex,
        int TotalFiles);
}
