namespace BusinessLogic.DTOs.Responses;

public sealed record CitationResponseDto(
    int CitationIndex,
    int DocumentId,
    int ChunkId,
    string DocumentTitle,
    int? PageNumber,
    int? SlideNumber,
    int? ChunkIndex,
    decimal? SimilarityScore,
    string? Snippet);
