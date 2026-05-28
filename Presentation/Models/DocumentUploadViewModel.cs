using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Presentation.Models;

public sealed class DocumentUploadViewModel
{
    [Display(Name = "Mã môn học")]
    [Required(ErrorMessage = "Vui lòng chọn môn học.")]
    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn môn học hợp lệ.")]
    public int? SubjectId { get; set; }

    [Display(Name = "Tiêu đề tài liệu")]
    [StringLength(255, ErrorMessage = "Tiêu đề không được vượt quá 255 ký tự.")]
    public string? Title { get; set; }

    [Display(Name = "Tài liệu")]
    [Required(ErrorMessage = "Vui lòng chọn tài liệu để tải lên.")]
    [MinLength(1, ErrorMessage = "Vui lòng chọn tài liệu để tải lên.")]
    public List<IFormFile> Files { get; set; } = [];

    public string UploadId { get; set; } = string.Empty;

    public int MaxFileSizeMb { get; set; } = 100;

    public int MaxFilesPerBatch { get; set; } = 10;

    public int MaxBatchSizeMb { get; set; } = 500;

    public IReadOnlyList<SubjectOptionViewModel> Subjects { get; set; } = [];
}
