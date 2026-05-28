using System;
using System.Collections.Generic;

namespace BusinessObject.Entities;

public partial class Citation
{
    public int Id { get; set; }

    public int ChatMessageId { get; set; }

    public int DocumentId { get; set; }

    public int ChunkId { get; set; }

    public int? PageNumber { get; set; }

    public int? SlideNumber { get; set; }

    public decimal? SimilarityScore { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ChatMessage ChatMessage { get; set; } = null!;

    public virtual DocumentChunk Chunk { get; set; } = null!;

    public virtual Document Document { get; set; } = null!;
}
