namespace ProductManagement.Api.DTOs.Auth;

public sealed record LoginResponse(
    string AccessToken,
    DateTime ExpiresAt,
    AuthUserResponse User);
