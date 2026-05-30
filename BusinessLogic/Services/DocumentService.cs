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
        var isStudentUpload = string.Equals(request.UploaderRole, UserRoleNames.Student, StringComparison.OrdinalIgnoreCase);
        var message = succeededCount == files.Count
            ? isStudentUpload
                ? $"Đã tải lên {succeededCount}/{files.Count} tài liệu. Tài liệu đang chờ giảng viên duyệt."
                : $"Đã tải lên và index thành công {succeededCount}/{files.Count} tài liệu."
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
        var subjects = await _subjectRepository.GetAllAsync(cancellationToken);
        var visibleSubjects = FilterDocumentLibrarySubjects(
            subjects,
            request.CurrentUserId,
            request.CurrentUserRole);
        var visibleSubjectIds = visibleSubjects.Select(subject => subject.Id).ToHashSet();

        var requestedSubjectId = request.SubjectId.HasValue && visibleSubjectIds.Contains(request.SubjectId.Value)
            ? request.SubjectId
            : null;

        var documents = await _documentRepository.GetListAsync(
            request.SearchTerm,
            requestedSubjectId,
            request.Status,
            cancellationToken);

        documents = documents
            .Where(document => visibleSubjectIds.Contains(document.SubjectId))
            .Where(document =>
                !string.Equals(request.CurrentUserRole, UserRoleNames.Student, StringComparison.OrdinalIgnoreCase)
                || document.Status == DocumentStatus.Indexed
                || document.UploadedBy == request.CurrentUserId)
            .ToList();

        return new DocumentListResultDto(
            documents.Select(MapListItem).ToList(),
            visibleSubjects
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

    public async Task<DocumentUploadResult> VerifyAndIndexAsync(
        int documentId,
        int verifiedBy,
        string? verifierRole,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document is null)
        {
            return new DocumentUploadResult(false, null, "Không tìm thấy tài liệu.");
        }

        if (document.Status is not DocumentStatus.Uploaded and not DocumentStatus.Failed)
        {
            return new DocumentUploadResult(false, document.Id, "Tài liệu này không ở trạng thái chờ duyệt.");
        }

        if (!await CanVerifyDocumentAsync(document.SubjectId, verifiedBy, verifierRole, cancellationToken))
        {
            return new DocumentUploadResult(false, document.Id, "Bạn không có quyền duyệt tài liệu cho môn học này.");
        }

        try
        {
            var additionalUserIds = document.UploadedBy.HasValue && document.UploadedBy.Value != verifiedBy
                ? new[] { document.UploadedBy.Value }
                : [];
            var progressContext = new UploadProgressContext(
                GetVerificationUploadId(document.Id),
                verifiedBy,
                1,
                1,
                additionalUserIds);

            await _documentRepository.UpdateStatusAsync(
                document.Id,
                DocumentStatus.Processing,
                cancellationToken: cancellationToken);

            if (document.Status == DocumentStatus.Failed && document.DocumentChunks.Count > 0)
            {
                return await FinalizeExistingChunksAsync(
                    document,
                    progressContext,
                    cancellationToken);
            }

            return await IndexDocumentAsync(
                document,
                document.FilePath,
                document.FileType,
                document.OriginalFileName,
                progressContext,
                cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Document verification/indexing failed for document {DocumentId}", documentId);
            var message = GetUserMessage(exception);
            await _documentRepository.UpdateStatusAsync(
                document.Id,
                DocumentStatus.Failed,
                message,
                cancellationToken: cancellationToken);
            return new DocumentUploadResult(false, document.Id, message);
        }
    }

    public async Task<DocumentUploadResult> RejectAsync(
        int documentId,
        int rejectedBy,
        string? rejecterRole,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken);
        if (document is null)
        {
            return new DocumentUploadResult(false, null, "Không tìm thấy tài liệu.");
        }

        if (document.Status is not DocumentStatus.Uploaded and not DocumentStatus.Failed)
        {
            return new DocumentUploadResult(false, document.Id, "Tài liệu này không ở trạng thái chờ duyệt.");
        }

        if (!await CanVerifyDocumentAsync(document.SubjectId, rejectedBy, rejecterRole, cancellationToken))
        {
            return new DocumentUploadResult(false, document.Id, "Bạn không có quyền từ chối tài liệu cho môn học này.");
        }

        var message = string.IsNullOrWhiteSpace(reason)
            ? "Tài liệu đã bị từ chối bởi giảng viên."
            : $"Từ chối: {reason.Trim()}";

        await _documentRepository.UpdateStatusAsync(
            document.Id,
            DocumentStatus.Rejected,
            message,
            cancellationToken: cancellationToken);

        return new DocumentUploadResult(true, document.Id, "Đã từ chối tài liệu.");
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
        var allSubjects = await _subjectRepository.GetAllAsync(cancellationToken);
        var subjects = string.Equals(userRole, UserRoleNames.Admin, StringComparison.OrdinalIgnoreCase)
            ? allSubjects
            : string.Equals(userRole, UserRoleNames.Teacher, StringComparison.OrdinalIgnoreCase)
                ? allSubjects
                    .Where(subject => IsTeacherParticipant(subject, userId))
                    .ToList()
                : string.Equals(userRole, UserRoleNames.Student, StringComparison.OrdinalIgnoreCase)
                    ? allSubjects
                        .Where(subject => subject.SubjectEnrollments.Any(enrollment =>
                            enrollment.UserId == userId
                            && string.Equals(enrollment.RoleInClass, UserRoleNames.Student, StringComparison.OrdinalIgnoreCase)))
                        .ToList()
                    : [];

        return subjects
            .Select(subject => new SubjectOptionDto(
                subject.Id,
                subject.SubjectCode,
                subject.SubjectName))
            .ToList();
    }

    private static IReadOnlyList<Subject> FilterDocumentLibrarySubjects(
        IReadOnlyList<Subject> subjects,
        int? currentUserId,
        string? currentUserRole)
    {
        if (string.Equals(currentUserRole, UserRoleNames.Student, StringComparison.OrdinalIgnoreCase))
        {
            return subjects
                .Where(subject => subject.SubjectEnrollments.Any(enrollment =>
                    enrollment.UserId == currentUserId
                    && string.Equals(enrollment.RoleInClass, UserRoleNames.Student, StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        if (string.Equals(currentUserRole, UserRoleNames.Teacher, StringComparison.OrdinalIgnoreCase))
        {
            return subjects
                .Where(subject => currentUserId.HasValue && IsTeacherParticipant(subject, currentUserId.Value))
                .ToList();
        }

        return subjects;
    }

    private static bool IsTeacherParticipant(Subject subject, int userId)
    {
        return subject.CreatedBy == userId
            || subject.SubjectEnrollments.Any(enrollment =>
                enrollment.UserId == userId
                && string.Equals(enrollment.RoleInClass, UserRoleNames.Teacher, StringComparison.OrdinalIgnoreCase));
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
                    Status = string.Equals(request.UploaderRole, UserRoleNames.Student, StringComparison.OrdinalIgnoreCase)
                        ? DocumentStatus.Uploaded
                        : DocumentStatus.Processing,
                    UploadedAt = DateTime.UtcNow
                },
                cancellationToken);

            if (string.Equals(request.UploaderRole, UserRoleNames.Student, StringComparison.OrdinalIgnoreCase))
            {
                await ReportProgressAsync(
                    progressContext,
                    request.FileName,
                    "completed",
                    100,
                    "Tài liệu đã được tải lên và đang chờ giảng viên duyệt.",
                    isCompleted: true,
                    cancellationToken: cancellationToken);

                return new DocumentUploadResult(
                    true,
                    document.Id,
                    "Tài liệu đã được tải lên và đang chờ giảng viên duyệt.");
            }

            return await IndexDocumentAsync(
                document,
                storedFile.FilePath,
                storedFile.FileType,
                request.FileName,
                progressContext,
                cancellationToken);
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
        Document document,
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

        var vectorChunks = chunks
            .Select(chunk => new DocumentChunk
            {
                Id = chunk.Id,
                DocumentId = chunk.DocumentId,
                Document = document,
                ChunkIndex = chunk.ChunkIndex,
                Content = chunk.Content,
                PageNumber = chunk.PageNumber,
                SlideNumber = chunk.SlideNumber,
                TokenCount = chunk.TokenCount,
                VectorId = chunk.VectorId,
                EmbeddingModel = chunk.EmbeddingModel,
                EmbeddingJson = chunk.EmbeddingJson,
                CreatedAt = chunk.CreatedAt
            })
            .ToList();

        await _vectorStoreService.UpsertAsync(
            embeddingService.ModelKey,
            embeddingService.ProviderName,
            vectorChunks,
            embeddings,
            cancellationToken);
    }

    private async Task<DocumentUploadResult> IndexDocumentAsync(
        Document document,
        string filePath,
        string fileType,
        string fileName,
        UploadProgressContext? progressContext,
        CancellationToken cancellationToken)
    {
        await ReportProgressAsync(
            progressContext,
            fileName,
            "extracting",
            35,
            "Đang đọc nội dung tài liệu...",
            cancellationToken: cancellationToken);

        var extractedSegments = await _textExtractionService.ExtractAsync(
            filePath,
            fileType,
            cancellationToken);

        await ReportProgressAsync(
            progressContext,
            fileName,
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
                fileName,
                "embedding",
                embeddingPercent,
                $"Đang tạo embedding ({index + 1}/{chunkDrafts.Count})...",
                cancellationToken: cancellationToken);
        }

        await ReportProgressAsync(
            progressContext,
            fileName,
            "indexing",
            90,
            "Đang lưu chunks và cập nhật trạng thái...",
            cancellationToken: cancellationToken);

        await _documentChunkRepository.AddRangeAsync(chunks, cancellationToken);
        await SaveEmbeddingsAsync(
            chunks,
            defaultEmbeddings,
            defaultEmbeddingService,
            document,
            cancellationToken);

        await _documentRepository.UpdateStatusAsync(
            document.Id,
            DocumentStatus.Indexed,
            indexedAt: DateTime.UtcNow,
            cancellationToken: cancellationToken);

        await ReportProgressAsync(
            progressContext,
            fileName,
            "completed",
            100,
            "Tài liệu đã được index thành công.",
            isCompleted: true,
            cancellationToken: cancellationToken);

        return new DocumentUploadResult(true, document.Id, "Tài liệu đã được tải lên và index thành công.");
    }

    private async Task<DocumentUploadResult> FinalizeExistingChunksAsync(
        Document document,
        UploadProgressContext? progressContext,
        CancellationToken cancellationToken)
    {
        var chunks = document.DocumentChunks
            .OrderBy(chunk => chunk.ChunkIndex)
            .ToList();
        var defaultEmbeddingService = _embeddingModelRegistry.GetDefault();
        var embeddings = chunks
            .Select(chunk => DeserializeEmbedding(chunk.EmbeddingJson))
            .ToList();

        if (chunks.Count == 0 || embeddings.Any(embedding => embedding.Length == 0))
        {
            throw new InvalidOperationException("Không thể index lại tài liệu lỗi. Vui lòng tải lại tài liệu.");
        }

        await ReportProgressAsync(
            progressContext,
            document.OriginalFileName,
            "indexing",
            90,
            "Đang hoàn tất index từ chunks đã xử lý...",
            cancellationToken: cancellationToken);

        await SaveEmbeddingsAsync(
            chunks,
            embeddings,
            defaultEmbeddingService,
            document,
            cancellationToken);

        await _documentRepository.UpdateStatusAsync(
            document.Id,
            DocumentStatus.Indexed,
            indexedAt: DateTime.UtcNow,
            cancellationToken: cancellationToken);

        await ReportProgressAsync(
            progressContext,
            document.OriginalFileName,
            "completed",
            100,
            "Tài liệu đã được index thành công.",
            isCompleted: true,
            cancellationToken: cancellationToken);

        return new DocumentUploadResult(true, document.Id, "Tài liệu đã được duyệt và index thành công.");
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

        if (string.Equals(uploaderRole, UserRoleNames.Admin, StringComparison.OrdinalIgnoreCase))
        {
            var subject = await _subjectRepository.GetByIdAsync(subjectId, cancellationToken);
            if (subject is null)
            {
                throw new InvalidOperationException("Vui lòng chọn môn học hợp lệ.");
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
        else if (string.Equals(uploaderRole, UserRoleNames.Student, StringComparison.OrdinalIgnoreCase))
        {
            var subject = await _subjectRepository.GetByIdAsync(subjectId, cancellationToken);
            var canUpload = subject is not null
                && subject.SubjectEnrollments.Any(enrollment =>
                    enrollment.UserId == uploadedBy.Value
                    && string.Equals(enrollment.RoleInClass, UserRoleNames.Student, StringComparison.OrdinalIgnoreCase));

            if (!canUpload)
            {
                throw new InvalidOperationException("Bạn cần tham gia môn học trước khi tải tài liệu lên.");
            }
        }
        else
        {
            throw new InvalidOperationException("Bạn không có quyền tải tài liệu.");
        }
    }

    private async Task<bool> CanVerifyDocumentAsync(
        int subjectId,
        int verifierId,
        string? verifierRole,
        CancellationToken cancellationToken)
    {
        if (string.Equals(verifierRole, UserRoleNames.Admin, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!string.Equals(verifierRole, UserRoleNames.Teacher, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var subject = await _subjectRepository.GetByIdAsync(subjectId, cancellationToken);
        return subject is not null && IsTeacherParticipant(subject, verifierId);
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
            var userIds = new[] { context.UserId }
                .Concat(context.AdditionalUserIds ?? [])
                .Distinct()
                .ToList();

            foreach (var userId in userIds)
            {
                await _uploadProgressReporter.ReportAsync(
                    new UploadProgressDto(
                        context.UploadId,
                        userId,
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
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Unable to publish upload progress for {FileName}", fileName);
        }
    }

    private static string GetUserMessage(Exception exception)
    {
        if (exception is InvalidOperationException
            && !IsTechnicalExceptionMessage(exception.Message))
        {
            return exception.Message;
        }

        return "Có lỗi khi xử lý tài liệu. Vui lòng thử lại hoặc tải lại tài liệu.";
    }

    private static bool IsTechnicalExceptionMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        return message.Contains("entity type", StringComparison.OrdinalIgnoreCase)
            || message.Contains("same key value", StringComparison.OrdinalIgnoreCase)
            || message.Contains("DbContext", StringComparison.OrdinalIgnoreCase)
            || message.Contains("System.", StringComparison.OrdinalIgnoreCase)
            || message.Contains("Microsoft.", StringComparison.OrdinalIgnoreCase);
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

    private static float[] DeserializeEmbedding(string? embeddingJson)
    {
        if (string.IsNullOrWhiteSpace(embeddingJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<float[]>(embeddingJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string GetVerificationUploadId(int documentId)
    {
        return $"verify-{documentId}";
    }

    private sealed record UploadProgressContext(
        string UploadId,
        int UserId,
        int FileIndex,
        int TotalFiles,
        IReadOnlyList<int>? AdditionalUserIds = null);
}
