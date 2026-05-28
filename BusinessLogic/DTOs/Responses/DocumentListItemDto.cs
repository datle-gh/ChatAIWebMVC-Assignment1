using BusinessObject.Enums;

namespace BusinessLogic.DTOs.Responses;

public sealed record DocumentListItemDto(
    int Id,
    int SubjectId,
    string SubjectName,
    string Title,
    string OriginalFileName,
    string FileType,
    long? FileSizeBytes,
    string? UploadedByName,
    DocumentStatus Status,
    string? ErrorMessage,
    DateTime UploadedAt,
    DateTime? IndexedAt,
    int ChunkCount,
    int? TotalTokenCount,
    string? EmbeddingModel,
    int? PreviewChunkIndex,
    string? PreviewContent);
