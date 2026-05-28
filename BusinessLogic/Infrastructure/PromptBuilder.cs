using System.Text;
using BusinessLogic.DTOs.Responses;

namespace BusinessLogic.Infrastructure;

public sealed class PromptBuilder
{
    public string Build(string question, IReadOnlyList<RetrievedChunkDto> chunks)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Bạn là chatbot học tập cho sinh viên Việt Nam.");
        builder.AppendLine("Nhiệm vụ của bạn là trả lời dựa trên ngữ cảnh tài liệu được cung cấp.");
        builder.AppendLine();
        builder.AppendLine("Câu hỏi của sinh viên:");
        builder.AppendLine(question.Trim());
        builder.AppendLine();
        builder.AppendLine("Ngữ cảnh tài liệu:");

        if (chunks.Count == 0)
        {
            builder.AppendLine("Không có chunk tài liệu liên quan.");
        }
        else
        {
            foreach (var (chunk, index) in chunks.Select((chunk, index) => (chunk, index)))
            {
                builder.AppendLine($"[Nguồn {index + 1}] {chunk.OriginalFileName}");
                builder.AppendLine($"Chunk: {chunk.ChunkIndex}");
                if (chunk.PageNumber.HasValue)
                {
                    builder.AppendLine($"Trang: {chunk.PageNumber.Value}");
                }

                if (chunk.SlideNumber.HasValue)
                {
                    builder.AppendLine($"Slide: {chunk.SlideNumber.Value}");
                }

                builder.AppendLine($"Độ tương đồng: {chunk.SimilarityScore:0.######}");
                builder.AppendLine("Nội dung:");
                builder.AppendLine(chunk.Content);
                builder.AppendLine();
            }
        }

        builder.AppendLine("Quy tắc trả lời:");
        builder.AppendLine("- Trả lời bằng tiếng Việt, rõ ràng, ngắn gọn và phù hợp bối cảnh học thuật.");
        builder.AppendLine("- Chỉ dùng thông tin trong ngữ cảnh tài liệu ở trên.");
        builder.AppendLine("- Nếu ngữ cảnh không đủ để trả lời, hãy nói: Không tìm thấy thông tin này trong tài liệu đã tải lên.");
        builder.AppendLine("- Không bịa nội dung, không suy đoán ngoài tài liệu.");
        builder.AppendLine("- Không tự tạo tên tài liệu, số trang, số slide hoặc nguồn tham khảo.");
        builder.AppendLine("- Không cần liệt kê nguồn ở cuối câu trả lời; hệ thống sẽ hiển thị nguồn tham khảo từ các chunk đã truy xuất.");

        return builder.ToString();
    }
}
