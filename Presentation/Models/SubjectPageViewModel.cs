using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

public sealed class SubjectPageViewModel
{
    public IReadOnlyList<SubjectViewModel> Subjects { get; set; } = [];
}

public sealed class SubjectViewModel
{
    public int Id { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DocumentCount { get; set; }
    public int IndexedDocumentCount { get; set; }
    public int StudentCount { get; set; }
    public int TeacherCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public IReadOnlyList<string> TeacherNames { get; set; } = [];
}

public sealed class CreateSubjectViewModel
{
    [Required(ErrorMessage = "Mã môn học là bắt buộc.")]
    [StringLength(50, ErrorMessage = "Mã môn học không được vượt quá 50 ký tự.")]
    [Display(Name = "Mã môn học")]
    public string SubjectCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên môn học là bắt buộc.")]
    [StringLength(200, ErrorMessage = "Tên môn học không được vượt quá 200 ký tự.")]
    [Display(Name = "Tên môn học")]
    public string SubjectName { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự.")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
}

public sealed class EditSubjectViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mã môn học là bắt buộc.")]
    [StringLength(50, ErrorMessage = "Mã môn học không được vượt quá 50 ký tự.")]
    [Display(Name = "Mã môn học")]
    public string SubjectCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tên môn học là bắt buộc.")]
    [StringLength(200, ErrorMessage = "Tên môn học không được vượt quá 200 ký tự.")]
    [Display(Name = "Tên môn học")]
    public string SubjectName { get; set; } = string.Empty;

    [StringLength(2000, ErrorMessage = "Mô tả không được vượt quá 2000 ký tự.")]
    [Display(Name = "Mô tả")]
    public string? Description { get; set; }
}
