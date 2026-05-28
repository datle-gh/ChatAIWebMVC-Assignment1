using System;
using System.Collections.Generic;

namespace BusinessObject.Entities;

public partial class DocumentChunk
{
    public int Id { get; set; }

    public int DocumentId { get; set; }

    public int ChunkIndex { get; set; }

    public string Content { get; set; } = null!;

    public int? PageNumber { get; set; }

    public int? SlideNumber { get; set; }

    public int? TokenCount { get; set; }

    public string? VectorId { get; set; }

    public string? EmbeddingModel { get; set; }

    public string? EmbeddingJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Citation> Citations { get; set; } = new List<Citation>();

    public virtual ICollection<DocumentChunkEmbedding> DocumentChunkEmbeddings { get; set; } = new List<DocumentChunkEmbedding>();

    public virtual Document Document { get; set; } = null!;
}
