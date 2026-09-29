using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.Auth;

public sealed class LoginRequest
{
    [Required(ErrorMessage = "Tên đăng nhập là bắt buộc.")]
    [StringLength(100, ErrorMessage = "Tên đăng nhập tối đa 100 ký tự.")]
    public string Username { get; init; } = string.Empty;

    [Required(ErrorMessage = "Mật khẩu là bắt buộc.")]
    [StringLength(200, ErrorMessage = "Mật khẩu tối đa 200 ký tự.")]
    public string Password { get; init; } = string.Empty;
}
