namespace ProductManagement.Api.Services.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string Username { get; }
    string FullName { get; }
    string Role { get; }
    string? IpAddress { get; }
}
