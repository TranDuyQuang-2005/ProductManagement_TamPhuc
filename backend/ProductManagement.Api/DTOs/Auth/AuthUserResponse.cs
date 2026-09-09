namespace ProductManagement.Api.DTOs.Auth;

public sealed record AuthUserResponse(
    string Id,
    string Username,
    string FullName,
    string Role);
