using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

public sealed class ProfileViewModel
{
    public int Id { get; set; }

    [Display(Name = "Họ tên")]
    [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
    [StringLength(150, ErrorMessage = "Họ tên không được vượt quá 150 ký tự.")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Email")]
    [Required(ErrorMessage = "Vui lòng nhập email.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    [StringLength(256, ErrorMessage = "Email không được vượt quá 256 ký tự.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Vai trò")]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Mật khẩu hiện tại")]
    [DataType(DataType.Password)]
    public string? CurrentPassword { get; set; }

    [Display(Name = "Mật khẩu mới")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự.")]
    public string? NewPassword { get; set; }

    [Display(Name = "Nhập lại mật khẩu mới")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    public string? ConfirmNewPassword { get; set; }
}
