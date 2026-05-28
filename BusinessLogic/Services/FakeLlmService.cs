using System.Text;
using System.Text.RegularExpressions;

namespace BusinessLogic.Services;

public sealed class FakeLlmService : ILlmService
{
    public string ModelName => "fake-llm-v1";

    public Task<string> GenerateAnswerAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var question = ExtractSection(prompt, "Câu hỏi của sinh viên:", "Ngữ cảnh tài liệu:");
        var context = ExtractSection(prompt, "Ngữ cảnh tài liệu:", "Quy tắc trả lời:");
        var snippets = Regex.Matches(context, @"Nội dung:\s*(?<content>.*?)(?=\n\n\[Chunk|\z)", RegexOptions.Singleline)
            .Select(match => NormalizeWhitespace(match.Groups["content"].Value))
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Take(2)
            .ToList();

        if (snippets.Count == 0)
        {
            return Task.FromResult("Không tìm thấy thông tin này trong tài liệu đã tải lên.");
        }

        var builder = new StringBuilder();
        builder.AppendLine("Theo tài liệu đã tải lên, có thể trả lời như sau:");
        builder.AppendLine();
        builder.AppendLine($"Câu hỏi: {question}");
        builder.AppendLine();

        foreach (var snippet in snippets)
        {
            builder.Append("- ");
            builder.AppendLine(snippet.Length > 350 ? $"{snippet[..350]}..." : snippet);
        }

        builder.AppendLine();
        builder.Append("Đây là câu trả lời mô phỏng từ FakeLlmService để kiểm tra luồng RAG trước khi kết nối mô hình AI thật.");

        return Task.FromResult(builder.ToString());
    }

    private static string ExtractSection(string text, string startMarker, string endMarker)
    {
        var start = text.IndexOf(startMarker, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return string.Empty;
        }

        start += startMarker.Length;
        var end = text.IndexOf(endMarker, start, StringComparison.OrdinalIgnoreCase);
        return (end < 0 ? text[start..] : text[start..end]).Trim();
    }

    private static string NormalizeWhitespace(string text)
    {
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
}
