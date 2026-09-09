namespace ProductManagement.Api.Configuration;

public sealed class StaffSeedOptions
{
    public string Username { get; init; } = "staff";
    public string Password { get; init; } = string.Empty;
    public string FullName { get; init; } = "Staff User";
    public string Email { get; init; } = "staff@example.local";
}
