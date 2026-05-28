using System.Text;
using BusinessLogic.Infrastructure;
using Microsoft.Extensions.Configuration;

namespace BusinessLogic.Services;

public sealed class ChunkingService : IChunkingService
{
    private readonly RagSettings _ragSettings;

    public ChunkingService(IConfiguration configuration)
    {
        _ragSettings = RagSettings.FromConfiguration(configuration);
    }

    public IReadOnlyList<DocumentChunkDraft> SplitIntoChunks(IReadOnlyList<ExtractedTextSegment> segments)
    {
        var chunks = new List<DocumentChunkDraft>();

        foreach (var segment in segments.Where(item => !string.IsNullOrWhiteSpace(item.Text)))
        {
            var paragraphs = segment.Text
                .Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var builder = new StringBuilder();
            var tokenCount = 0;

            foreach (var paragraph in paragraphs)
            {
                var paragraphTokens = CountApproximateTokens(paragraph);

                if (builder.Length > 0 && tokenCount + paragraphTokens > _ragSettings.MaxChunkTokens)
                {
                    AddChunk(chunks, builder.ToString(), segment.PageNumber, segment.SlideNumber, tokenCount);
                    var overlap = CreateOverlap(builder.ToString());
                    builder.Clear();
                    tokenCount = CountApproximateTokens(overlap);
                    if (!string.IsNullOrWhiteSpace(overlap))
                    {
                        builder.Append(overlap);
                    }
                }

                if (paragraphTokens > _ragSettings.MaxChunkTokens)
                {
                    foreach (var slice in SplitLongParagraph(paragraph))
                    {
                        AddChunk(chunks, slice, segment.PageNumber, segment.SlideNumber, CountApproximateTokens(slice));
                    }

                    builder.Clear();
                    tokenCount = 0;
                    continue;
                }

                if (builder.Length > 0)
                {
                    builder.AppendLine();
                    builder.AppendLine();
                }

                builder.Append(paragraph);
                tokenCount += paragraphTokens;
            }

            if (builder.Length > 0)
            {
                AddChunk(chunks, builder.ToString(), segment.PageNumber, segment.SlideNumber, tokenCount);
            }
        }

        return chunks;
    }

    private IEnumerable<string> SplitLongParagraph(string paragraph)
    {
        var words = paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var builder = new StringBuilder();
        var tokenCount = 0;

        foreach (var word in words)
        {
            if (tokenCount >= _ragSettings.MaxChunkTokens)
            {
                yield return builder.ToString();
                var overlap = CreateOverlap(builder.ToString());
                builder.Clear();
                tokenCount = CountApproximateTokens(overlap);
                if (!string.IsNullOrWhiteSpace(overlap))
                {
                    builder.Append(overlap);
                }
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(word);
            tokenCount++;
        }

        if (builder.Length > 0)
        {
            yield return builder.ToString();
        }
    }

    private void AddChunk(
        ICollection<DocumentChunkDraft> chunks,
        string content,
        int? pageNumber,
        int? slideNumber,
        int tokenCount)
    {
        var cleanedContent = content.Trim();
        if (cleanedContent.Length < _ragSettings.MinChunkCharacters)
        {
            return;
        }

        chunks.Add(new DocumentChunkDraft(
            chunks.Count,
            cleanedContent,
            pageNumber,
            slideNumber,
            tokenCount));
    }

    private static int CountApproximateTokens(string text)
    {
        return text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;
    }

    private string CreateOverlap(string content)
    {
        if (_ragSettings.ChunkOverlapTokens <= 0)
        {
            return string.Empty;
        }

        var words = content.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (words.Length == 0)
        {
            return string.Empty;
        }

        var overlapSize = Math.Min(_ragSettings.ChunkOverlapTokens, Math.Max(0, _ragSettings.MaxChunkTokens / 2));
        return string.Join(' ', words.TakeLast(Math.Min(words.Length, overlapSize)));
    }
}
