using System.ComponentModel.DataAnnotations;

namespace ProductManagement.Api.DTOs.Auth;

public sealed class LoginRequest
{
    [Required, StringLength(100)]
    public string Username { get; init; } = string.Empty;

    [Required, StringLength(200)]
    public string Password { get; init; } = string.Empty;
}
