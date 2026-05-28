using System;
using System.Collections.Generic;
using BusinessObject.Enums;

namespace BusinessObject.Entities;

public partial class Document
{
    public int Id { get; set; }

    public int SubjectId { get; set; }

    public string Title { get; set; } = null!;

    public string OriginalFileName { get; set; } = null!;

    public string StoredFileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string FileType { get; set; } = null!;

    public long? FileSizeBytes { get; set; }

    public int? UploadedBy { get; set; }

    public DocumentStatus Status { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime UploadedAt { get; set; }

    public DateTime? IndexedAt { get; set; }

    public virtual ICollection<Citation> Citations { get; set; } = new List<Citation>();

    public virtual ICollection<DocumentChunk> DocumentChunks { get; set; } = new List<DocumentChunk>();

    public virtual Subject Subject { get; set; } = null!;

    public virtual User? UploadedByNavigation { get; set; }
}
