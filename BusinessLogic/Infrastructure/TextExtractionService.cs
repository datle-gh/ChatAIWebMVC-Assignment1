using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using A = DocumentFormat.OpenXml.Drawing;

namespace BusinessLogic.Infrastructure;

public sealed class TextExtractionService : ITextExtractionService
{
    public Task<IReadOnlyList<ExtractedTextSegment>> ExtractAsync(
        string filePath,
        string fileType,
        CancellationToken cancellationToken = default)
    {
        var segments = fileType.ToUpperInvariant() switch
        {
            "PDF" => ExtractPdf(filePath),
            "DOCX" => ExtractDocx(filePath),
            "PPTX" => ExtractPptx(filePath),
            _ => throw new InvalidOperationException("File không đúng định dạng được hỗ trợ.")
        };

        if (segments.Count == 0 || segments.All(segment => string.IsNullOrWhiteSpace(segment.Text)))
        {
            throw new InvalidOperationException("Không thể đọc nội dung tài liệu.");
        }

        return Task.FromResult<IReadOnlyList<ExtractedTextSegment>>(segments);
    }

    private static List<ExtractedTextSegment> ExtractPdf(string filePath)
    {
        using var document = PdfDocument.Open(filePath);
        var segments = new List<ExtractedTextSegment>();
        var totalLetterCount = 0;
        var totalImageCount = 0;

        foreach (var page in document.GetPages())
        {
            totalLetterCount += page.Letters.Count;
            totalImageCount += page.GetImages().Count();

            var text = ExtractPdfPageText(page);
            if (!string.IsNullOrWhiteSpace(text))
            {
                segments.Add(new ExtractedTextSegment(text, PageNumber: page.Number));
            }
        }

        if (segments.Count == 0 && totalLetterCount == 0 && totalImageCount > 0)
        {
            throw new InvalidOperationException("PDF này có thể là bản scan hoặc hình ảnh, cần OCR trước khi index.");
        }

        return segments;
    }

    private static string ExtractPdfPageText(Page page)
    {
        var text = NormalizeExtractedText(page.Text);
        if (IsMeaningfulText(text))
        {
            return text;
        }

        text = NormalizeExtractedText(string.Join(' ', page.GetWords().Select(word => word.Text)));
        if (IsMeaningfulText(text))
        {
            return text;
        }

        text = NormalizeExtractedText(ExtractTextFromLetters(page.Letters));
        return IsMeaningfulText(text) ? text : string.Empty;
    }

    private static string ExtractTextFromLetters(IReadOnlyList<Letter> letters)
    {
        if (letters.Count == 0)
        {
            return string.Empty;
        }

        var lines = letters
            .Where(letter => !string.IsNullOrWhiteSpace(letter.Value))
            .GroupBy(letter => Math.Round(letter.Location.Y / 2) * 2)
            .OrderByDescending(group => group.Key);

        var builder = new StringBuilder();
        foreach (var line in lines)
        {
            var orderedLetters = line
                .OrderBy(letter => letter.Location.X)
                .ToList();

            Letter? previous = null;
            foreach (var letter in orderedLetters)
            {
                if (previous is not null && ShouldInsertSpace(previous, letter))
                {
                    builder.Append(' ');
                }

                builder.Append(letter.Value);
                previous = letter;
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static bool ShouldInsertSpace(Letter previous, Letter current)
    {
        var gap = current.Location.X - previous.EndBaseLine.X;
        var expectedSpace = Math.Max(previous.Width, previous.FontSize * 0.25);

        return gap > expectedSpace;
    }

    private static List<ExtractedTextSegment> ExtractDocx(string filePath)
    {
        using var document = WordprocessingDocument.Open(filePath, false);
        var bodyText = document.MainDocumentPart?.Document.Body?.InnerText;
        var text = NormalizeExtractedText(bodyText);

        return string.IsNullOrWhiteSpace(text)
            ? []
            : [new ExtractedTextSegment(text)];
    }

    private static List<ExtractedTextSegment> ExtractPptx(string filePath)
    {
        using var document = PresentationDocument.Open(filePath, false);
        var presentationPart = document.PresentationPart;
        if (presentationPart?.Presentation.SlideIdList is null)
        {
            return [];
        }

        var segments = new List<ExtractedTextSegment>();
        var slideNumber = 1;

        foreach (var slideId in presentationPart.Presentation.SlideIdList.Elements<SlideId>())
        {
            var relationshipId = slideId.RelationshipId?.Value;
            if (string.IsNullOrWhiteSpace(relationshipId))
            {
                continue;
            }

            var slidePart = (SlidePart)presentationPart.GetPartById(relationshipId);
            var text = NormalizeExtractedText(string.Join(
                Environment.NewLine,
                slidePart.Slide.Descendants<A.Text>().Select(item => item.Text)));

            if (!string.IsNullOrWhiteSpace(text))
            {
                segments.Add(new ExtractedTextSegment(text, SlideNumber: slideNumber));
            }

            slideNumber++;
        }

        return segments;
    }

    private static bool IsMeaningfulText(string text)
    {
        return text.Count(char.IsLetterOrDigit) >= 20;
    }

    private static string NormalizeExtractedText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        return Regex.Replace(text, @"[ \t\r\f\v]+", " ")
            .Replace("\n ", "\n", StringComparison.Ordinal)
            .Trim();
    }
}
