using ProductManagement.Api.Security;
using ProductManagement.Api.Services.Interfaces;

namespace ProductManagement.Api.Tests.Support;

internal sealed class AdminCurrentUserService : ICurrentUserService
{
    public string? UserId => "test-admin-id";
    public string Username => "admin";
    public string FullName => "Integration Test Admin";
    public string Role => AppRoles.Admin;
    public string? IpAddress => "127.0.0.1";
}
