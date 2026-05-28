namespace BusinessLogic.DTOs.Responses;

public sealed record RetrievedChunkDto(
    int ChunkId,
    int DocumentId,
    string DocumentTitle,
    string OriginalFileName,
    int ChunkIndex,
    string Content,
    int? PageNumber,
    int? SlideNumber,
    decimal SimilarityScore,
    string? EmbeddingModel = null,
    string? RetrievalBackend = null);
