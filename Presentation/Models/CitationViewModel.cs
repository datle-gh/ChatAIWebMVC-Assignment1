namespace Presentation.Models;

public sealed class CitationViewModel
{
    public int CitationIndex { get; set; }

    public string DocumentTitle { get; set; } = string.Empty;

    public int? PageNumber { get; set; }

    public int? SlideNumber { get; set; }

    public int? ChunkIndex { get; set; }

    public decimal? SimilarityScore { get; set; }

    public string? Snippet { get; set; }
}
