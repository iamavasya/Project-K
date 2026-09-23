using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Authorization;

namespace ProjectK.Infrastructure.Seeding;

/// <summary>
/// Takes office roles out of the identity store. They used to be written onto the account by a
/// sync service, which is what made "виховник" something a person was everywhere rather than in
/// one kurin; they are now worked out per kurin when a token is minted, and the rows left behind
/// say something that is no longer true.
/// <para>
/// Idempotent, and a no-op once the store is clean. <see cref="SystemRole.Admin"/> and
/// <see cref="SystemRole.Member"/> are left alone — those are the two the store still holds.
/// </para>
/// </summary>
public static class OfficeRoleCleanupSeeder
{
    public static async Task CleanAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

        var officeRoles = SystemRole.All()
            .Where(role => role != SystemRole.Admin && role != SystemRole.Member)
            .ToArray();

        var found = new List<string>();
        foreach (var role in officeRoles)
        {
            if (await roleManager.RoleExistsAsync(role))
            {
                found.Add(role);
            }
        }

        if (found.Count == 0)
        {
            return;
        }

        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(nameof(OfficeRoleCleanupSeeder));
        logger?.LogWarning(
            "Office roles are still stored on accounts ({Count}); removing them — access now comes from the office registry.",
            found.Count);

        foreach (var role in found)
        {
            foreach (var user in await userManager.GetUsersInRoleAsync(role))
            {
                await userManager.RemoveFromRoleAsync(user, role);
            }

            var entity = await roleManager.FindByNameAsync(role);
            if (entity is not null)
            {
                await roleManager.DeleteAsync(entity);
            }
        }
    }
}
