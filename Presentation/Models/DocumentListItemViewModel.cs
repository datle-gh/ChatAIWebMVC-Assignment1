using BusinessObject.Enums;

namespace Presentation.Models;

public sealed class DocumentListItemViewModel
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public string SubjectName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string FileType { get; set; } = string.Empty;

    public long? FileSizeBytes { get; set; }

    public string? UploadedByName { get; set; }

    public DocumentStatus Status { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime? IndexedAt { get; set; }

    public int ChunkCount { get; set; }

    public int? TotalTokenCount { get; set; }

    public string? EmbeddingModel { get; set; }

    public int? PreviewChunkIndex { get; set; }

    public string? PreviewContent { get; set; }
}
