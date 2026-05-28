using System;

namespace BusinessObject.Entities;

public partial class DocumentChunkEmbedding
{
    public int Id { get; set; }

    public int DocumentChunkId { get; set; }

    public string EmbeddingModel { get; set; } = null!;

    public string EmbeddingProvider { get; set; } = null!;

    public int Dimension { get; set; }

    public string? VectorId { get; set; }

    public string? VectorStore { get; set; }

    public string? EmbeddingJson { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual DocumentChunk DocumentChunk { get; set; } = null!;
}
