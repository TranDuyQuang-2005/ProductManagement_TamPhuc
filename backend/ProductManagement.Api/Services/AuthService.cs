using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ProductManagement.Api.Common;
using ProductManagement.Api.Configuration;
using ProductManagement.Api.Data;
using ProductManagement.Api.DTOs.Auth;
using ProductManagement.Api.Entities;
using ProductManagement.Api.Security;
using ProductManagement.Api.Services.Interfaces;

namespace ProductManagement.Api.Services;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    AppDbContext dbContext,
    IOptions<JwtSettings> jwtOptions,
    IAuditService auditService) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var user = await userManager.FindByNameAsync(username);

        if (user is null || !user.IsActive)
        {
            await auditService.AddAuthAsync("LOGIN_FAILED", user, username, "Dang nhap that bai.", cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw AppException.BadRequest("Ten dang nhap hoac mat khau khong chinh xac.", "username", "Thong tin dang nhap khong hop le.");
        }

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await auditService.AddAuthAsync("LOGIN_FAILED", user, username, "Dang nhap that bai.", cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw AppException.BadRequest("Ten dang nhap hoac mat khau khong chinh xac.", "password", "Thong tin dang nhap khong hop le.");
        }

        var role = await GetPrimaryRoleAsync(user);
        if (role is null)
        {
            await auditService.AddAuthAsync("LOGIN_FAILED", user, username, "Vai tro tai khoan khong hop le.", cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            throw AppException.Forbidden("Tai khoan chua duoc gan vai tro hop le.");
        }

        var now = DateTime.UtcNow;
        user.LastLoginAt = now;
        await userManager.UpdateAsync(user);
        await auditService.AddAuthAsync("LOGIN", user, username, "Dang nhap he thong.", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var expiresAt = now.AddMinutes(jwtOptions.Value.ExpirationMinutes);
        var accessToken = CreateToken(user, role, expiresAt);
        return new LoginResponse(
            accessToken,
            expiresAt,
            new AuthUserResponse(user.Id, user.UserName ?? username, user.FullName, role));
    }

    private async Task<string?> GetPrimaryRoleAsync(ApplicationUser user)
        => (await userManager.GetRolesAsync(user))
            .Select(NormalizeRole)
            .FirstOrDefault(x => x is not null);

    private static string? NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return null;
        var normalized = role.Trim().ToUpperInvariant();
        return normalized is AppRoles.Admin or AppRoles.Staff ? normalized : null;
    }

    private string CreateToken(ApplicationUser user, string role, DateTime expiresAt)
    {
        var settings = jwtOptions.Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new("userId", user.Id),
            new("username", user.UserName ?? string.Empty),
            new("fullName", user.FullName),
            new(ClaimTypes.Role, role),
            new("role", role)
        };

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
