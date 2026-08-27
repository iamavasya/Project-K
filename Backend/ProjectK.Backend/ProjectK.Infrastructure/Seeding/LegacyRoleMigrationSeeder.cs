using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.DbContexts;
using ProjectK.Common.Interfaces.Modules.AuthModule;

namespace ProjectK.Infrastructure.Seeding
{
    /// <summary>
    /// One-off, idempotent back-fill that moves users off the old flat Identity roles
    /// (Manager/Mentor/User) onto the office-driven <see cref="SystemRole"/> model. A former Manager
    /// becomes the kurin's Зв'язковий; a Mentor keeps their group access through their existing
    /// assignments; a plain User falls back to <see cref="SystemRole.Member"/>. Safe to run on every
    /// startup — it no-ops once the legacy roles are gone.
    /// </summary>
    public static class LegacyRoleMigrationSeeder
    {
        private const string LegacyManager = "Manager";
        private const string LegacyMentor = "Mentor";
        private const string LegacyUser = "User";
        private static readonly string[] LegacyRoles = { LegacyManager, LegacyMentor, LegacyUser };

        public static async Task MigrateAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleSync = scope.ServiceProvider.GetRequiredService<ILeadershipRoleSyncService>();

            var presentLegacyRoles = new List<string>();
            foreach (var role in LegacyRoles)
            {
                if (await roleManager.RoleExistsAsync(role))
                {
                    presentLegacyRoles.Add(role);
                }
            }

            if (presentLegacyRoles.Count == 0)
            {
                return;
            }

            // 1. Former Managers become the kurin's Зв'язковий (KV office).
            if (presentLegacyRoles.Contains(LegacyManager))
            {
                foreach (var user in await userManager.GetUsersInRoleAsync(LegacyManager))
                {
                    var member = await unitOfWork.Members.GetByUserKeyAsync(user.Id);
                    if (member is not null)
                    {
                        await EnsureKvZvyazkovyiOfficeAsync(dbContext, member.MemberKey, member.KurinKey);
                    }
                }
            }

            // 2. Sync every affected user: offices + mentor assignments become system roles.
            var affectedUserIds = new HashSet<Guid>();
            foreach (var role in presentLegacyRoles)
            {
                foreach (var user in await userManager.GetUsersInRoleAsync(role))
                {
                    affectedUserIds.Add(user.Id);
                }
            }

            foreach (var userId in affectedUserIds)
            {
                var member = await unitOfWork.Members.GetByUserKeyAsync(userId);
                if (member is not null)
                {
                    await roleSync.SyncMemberAsync(member.MemberKey);
                }
                else
                {
                    var user = await userManager.FindByIdAsync(userId.ToString());
                    if (user is not null && !await userManager.IsInRoleAsync(user, SystemRole.Member))
                    {
                        await userManager.AddToRoleAsync(user, SystemRole.Member);
                    }
                }
            }

            // 3. Strip the legacy role names and delete the roles themselves.
            foreach (var role in presentLegacyRoles)
            {
                foreach (var user in await userManager.GetUsersInRoleAsync(role))
                {
                    await userManager.RemoveFromRoleAsync(user, role);
                }

                var roleEntity = await roleManager.FindByNameAsync(role);
                if (roleEntity is not null)
                {
                    await roleManager.DeleteAsync(roleEntity);
                }
            }
        }

        private static async Task EnsureKvZvyazkovyiOfficeAsync(AppDbContext dbContext, Guid memberKey, Guid kurinKey)
        {
            var leadership = await dbContext.Leaderships
                .Include(l => l.LeadershipHistories)
                .FirstOrDefaultAsync(l => l.Type == LeadershipType.KV && l.KurinKey == kurinKey);

            if (leadership is null)
            {
                leadership = new Leadership
                {
                    Type = LeadershipType.KV,
                    KurinKey = kurinKey,
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow)
                };
                dbContext.Leaderships.Add(leadership);
            }

            var alreadyHolds = leadership.LeadershipHistories
                .Any(h => h.MemberKey == memberKey && h.Role == LeadershipRole.Zvyazkovyi && h.EndDate == null);
            if (!alreadyHolds)
            {
                leadership.LeadershipHistories.Add(new LeadershipHistory
                {
                    MemberKey = memberKey,
                    Role = LeadershipRole.Zvyazkovyi,
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow)
                });
            }

            await dbContext.SaveChangesAsync();
        }
    }
}
