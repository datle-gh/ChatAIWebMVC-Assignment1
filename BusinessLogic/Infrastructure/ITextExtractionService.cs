namespace BusinessLogic.Infrastructure;

public interface ITextExtractionService
{
    Task<IReadOnlyList<ExtractedTextSegment>> ExtractAsync(
        string filePath,
        string fileType,
        CancellationToken cancellationToken = default);
}

public sealed record ExtractedTextSegment(
    string Text,
    int? PageNumber = null,
    int? SlideNumber = null);
