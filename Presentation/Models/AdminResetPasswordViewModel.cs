using System.ComponentModel.DataAnnotations;

namespace Presentation.Models;

public sealed class AdminResetPasswordViewModel
{
    public int UserId { get; set; }

    public string UserDisplayName { get; set; } = string.Empty;

    [Display(Name = "Mật khẩu mới")]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
    [MinLength(8, ErrorMessage = "Mật khẩu mới phải có ít nhất 8 ký tự.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Display(Name = "Nhập lại mật khẩu mới")]
    [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu mới.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Mật khẩu nhập lại không khớp.")]
    [DataType(DataType.Password)]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}
