using System.Security.Claims;
using ProductManagement.Api.Services.Interfaces;

namespace ProductManagement.Api.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirstValue("userId") ?? User?.FindFirstValue(ClaimTypes.NameIdentifier);
    public string Username => User?.FindFirstValue("username") ?? User?.Identity?.Name ?? "system";
    public string FullName => User?.FindFirstValue("fullName") ?? string.Empty;
    public string Role => User?.FindFirstValue(ClaimTypes.Role) ?? User?.FindFirstValue("role") ?? string.Empty;
    public string? IpAddress => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
