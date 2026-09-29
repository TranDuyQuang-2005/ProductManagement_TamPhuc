using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using ProductManagement.Api.Configuration;
using ProductManagement.Api.Entities;
using ProductManagement.Api.Security;

namespace ProductManagement.Api.Data;

public sealed class StaffUserSeeder(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IOptions<StaffSeedOptions> options,
    ILogger<StaffUserSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await roleManager.RoleExistsAsync(AppRoles.Staff))
            await roleManager.CreateAsync(new IdentityRole(AppRoles.Staff));

        var seed = options.Value;
        if (string.IsNullOrWhiteSpace(seed.Password))
        {
            logger.LogWarning("Staff seed password is empty. Skipping development staff seed.");
            return;
        }

        var username = seed.Username.Trim();
        var user = await userManager.FindByNameAsync(username);
        if (user is not null)
        {
            if (!await userManager.IsInRoleAsync(user, AppRoles.Staff))
                await userManager.AddToRoleAsync(user, AppRoles.Staff);
            return;
        }

        user = new ApplicationUser
        {
            UserName = username,
            Email = string.IsNullOrWhiteSpace(seed.Email) ? null : seed.Email.Trim(),
            FullName = string.IsNullOrWhiteSpace(seed.FullName) ? "Staff User" : seed.FullName.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, seed.Password);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(x => x.Description));
            throw new InvalidOperationException($"Failed to seed staff user: {errors}");
        }

        await userManager.AddToRoleAsync(user, AppRoles.Staff);
    }
}
