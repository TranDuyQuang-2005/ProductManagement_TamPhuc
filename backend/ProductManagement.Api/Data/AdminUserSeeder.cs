using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ProductManagement.Api.Configuration;
using ProductManagement.Api.Entities;
using ProductManagement.Api.Security;

namespace ProductManagement.Api.Data;

public sealed class AdminUserSeeder(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<AdminSeedOptions> options,
    ILogger<AdminUserSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        var seed = options.Value;
        if (string.IsNullOrWhiteSpace(seed.Password))
        {
            logger.LogWarning("Admin seed password is empty. Skipping development admin seed.");
            return;
        }

        var username = seed.Username.Trim();
        var user = await userManager.FindByNameAsync(username);
        if (user is not null)
        {
            if (!string.Equals(user.Role, AppRoles.Admin, StringComparison.OrdinalIgnoreCase))
            {
                user.Role = AppRoles.Admin;
                await userManager.UpdateAsync(user);
            }

            if (!await userManager.IsInRoleAsync(user, AppRoles.Admin))
                await userManager.AddToRoleAsync(user, AppRoles.Admin);
            return;
        }

        user = new ApplicationUser
        {
            UserName = username,
            Email = string.IsNullOrWhiteSpace(seed.Email) ? null : seed.Email.Trim(),
            FullName = string.IsNullOrWhiteSpace(seed.FullName) ? "Administrator" : seed.FullName.Trim(),
            Role = AppRoles.Admin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, seed.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Failed to seed admin user: {errors}");
        }

        await userManager.AddToRoleAsync(user, AppRoles.Admin);
    }
}
