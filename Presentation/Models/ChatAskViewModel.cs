using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

public sealed class ChatAskViewModel
{
    [Display(Name = "Mã môn học")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng nhập mã môn học hợp lệ.")]
    public int SubjectId { get; set; }

    public int? ChatSessionId { get; set; }

    [Display(Name = "Câu hỏi")]
    [Required(ErrorMessage = "Vui lòng nhập câu hỏi.")]
    public string Question { get; set; } = string.Empty;
}
