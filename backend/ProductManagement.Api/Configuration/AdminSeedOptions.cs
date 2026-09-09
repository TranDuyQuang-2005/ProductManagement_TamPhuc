namespace ProductManagement.Api.Configuration;

public sealed class AdminSeedOptions
{
    public string Username { get; init; } = "admin";
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = "Administrator";
    public string Email { get; init; } = "admin@example.local";
}
