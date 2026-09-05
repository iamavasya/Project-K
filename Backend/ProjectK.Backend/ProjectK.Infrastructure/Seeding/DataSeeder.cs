using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Seeding
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

            // 1. Seed Roles (Required for all environments): Admin, Member and every office role.
            foreach (var roleName in SystemRole.All())
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new AppRole(roleName));
                }
            }

            // 2. Seed System Admin (Skip for SelfHost, as it will be created via Setup)
            if (env.EnvironmentName != "SelfHost")
            {
                await EnsureUser(userManager, "admin@projectk.com", "System", "Admin", UserRole.Admin, "Admin@12345");
            }

            // 3. Seed Load Test User (Required for load tests across all environments)
            if (env.EnvironmentName != "SelfHost")
            {
                await EnsurePasswordlessUser(userManager, "loadtest@projectk.com", "Load", "Tester", UserRole.Member);
            }

            // --- STOP HERE FOR PRODUCTION AND SELFHOST ---
            // Only seed comprehensive test data in Development or other non-prod environments
            if (env.IsProduction() || env.EnvironmentName == "SelfHost")
            {
                return;
            }

            await ResetKurin1DataAsync(dbContext, userManager);

            // 4. Seed comprehensive demo data (kurin, groups, mentors, members)
            var demoSeeder = scope.ServiceProvider.GetRequiredService<IDemoDataSeeder>();
            await demoSeeder.SeedAsync();

            // Named fixtures the Playwright suite addresses directly. E2E only: they live in their own
            // kurin and must not leak into demo data for other environments.
            if (env.EnvironmentName == "E2E")
            {
                await E2eFixtureSeeder.SeedAsync(scope.ServiceProvider);
            }
        }

        private static async Task ResetKurin1DataAsync(AppDbContext dbContext, UserManager<AppUser> userManager)
        {
            var kurin1 = await dbContext.Kurins.FirstOrDefaultAsync(k => k.Number == 1);
            if (kurin1 == null)
            {
                return;
            }

            var kurinKey = kurin1.KurinKey;
            var groupKeys = await dbContext.Groups
                .Where(g => g.KurinKey == kurinKey)
                .Select(g => g.GroupKey)
                .ToListAsync();

            var memberKeys = await dbContext.Members
                .Where(m => m.KurinKey == kurinKey)
                .Select(m => m.MemberKey)
                .ToListAsync();

            var usersToDelete = await userManager.Users
                .Where(u => u.KurinKey == kurinKey && u.Email != "admin@projectk.com")
                .ToListAsync();

            foreach (var user in usersToDelete)
            {
                await userManager.DeleteAsync(user);
            }

            var planningSessionKeys = await dbContext.PlanningSessions
                .Where(s => s.KurinKey == kurinKey)
                .Select(s => s.PlanningSessionKey)
                .ToListAsync();

            var planningParticipantKeys = await dbContext.PlanningParticipants
                .Where(p => planningSessionKeys.Contains(p.PlanningSessionKey))
                .Select(p => p.PlanningParticipantKey)
                .ToListAsync();

            if (planningParticipantKeys.Count > 0)
            {
                var busyRanges = await dbContext.ParticipantBusyRanges
                    .Where(r => planningParticipantKeys.Contains(r.PlanningParticipantKey))
                    .ToListAsync();
                dbContext.ParticipantBusyRanges.RemoveRange(busyRanges);
            }

            if (planningSessionKeys.Count > 0)
            {
                var planningParticipants = await dbContext.PlanningParticipants
                    .Where(p => planningSessionKeys.Contains(p.PlanningSessionKey))
                    .ToListAsync();
                dbContext.PlanningParticipants.RemoveRange(planningParticipants);

                var planningSessions = await dbContext.PlanningSessions
                    .Where(s => s.KurinKey == kurinKey)
                    .ToListAsync();
                dbContext.PlanningSessions.RemoveRange(planningSessions);
            }

            var badgeProgressKeys = await dbContext.BadgeProgresses
                .Where(p => p.KurinKey == kurinKey || memberKeys.Contains(p.MemberKey))
                .Select(p => p.BadgeProgressKey)
                .ToListAsync();

            if (badgeProgressKeys.Count > 0)
            {
                var badgeAuditEvents = await dbContext.BadgeProgressAuditEvents
                    .Where(e => badgeProgressKeys.Contains(e.BadgeProgressKey))
                    .ToListAsync();
                dbContext.BadgeProgressAuditEvents.RemoveRange(badgeAuditEvents);
            }

            var badgeProgresses = await dbContext.BadgeProgresses
                .Where(p => p.KurinKey == kurinKey || memberKeys.Contains(p.MemberKey))
                .ToListAsync();
            dbContext.BadgeProgresses.RemoveRange(badgeProgresses);

            var probeProgressKeys = await dbContext.ProbeProgresses
                .Where(p => p.KurinKey == kurinKey || memberKeys.Contains(p.MemberKey))
                .Select(p => p.ProbeProgressKey)
                .ToListAsync();

            if (probeProgressKeys.Count > 0)
            {
                var probeAuditEvents = await dbContext.ProbeProgressAuditEvents
                    .Where(e => probeProgressKeys.Contains(e.ProbeProgressKey))
                    .ToListAsync();
                dbContext.ProbeProgressAuditEvents.RemoveRange(probeAuditEvents);
            }

            var probePointProgresses = await dbContext.ProbePointProgresses
                .Where(p => p.KurinKey == kurinKey || memberKeys.Contains(p.MemberKey))
                .ToListAsync();
            dbContext.ProbePointProgresses.RemoveRange(probePointProgresses);

            var probeProgresses = await dbContext.ProbeProgresses
                .Where(p => p.KurinKey == kurinKey || memberKeys.Contains(p.MemberKey))
                .ToListAsync();
            dbContext.ProbeProgresses.RemoveRange(probeProgresses);

            var plastLevelHistories = await dbContext.PlastLevelHistories
                .Where(p => memberKeys.Contains(p.MemberKey))
                .ToListAsync();
            dbContext.PlastLevelHistories.RemoveRange(plastLevelHistories);

            var memberAwards = await dbContext.MemberAwards
                .Where(a => a.KurinKey == kurinKey || memberKeys.Contains(a.MemberKey))
                .ToListAsync();
            dbContext.MemberAwards.RemoveRange(memberAwards);

            var memberWarnings = await dbContext.MemberWarnings
                .Where(w => memberKeys.Contains(w.MemberKey))
                .ToListAsync();
            dbContext.MemberWarnings.RemoveRange(memberWarnings);

            var leadershipKeys = await dbContext.Leaderships
                .Where(l => l.KurinKey == kurinKey || (l.GroupKey != null && groupKeys.Contains(l.GroupKey.Value)))
                .Select(l => l.LeadershipKey)
                .ToListAsync();

            if (leadershipKeys.Count > 0)
            {
                var leadershipHistories = await dbContext.LeadershipHistories
                    .Where(h => memberKeys.Contains(h.MemberKey) || leadershipKeys.Contains(h.LeadershipKey))
                    .ToListAsync();
                dbContext.LeadershipHistories.RemoveRange(leadershipHistories);
            }

            var leaderships = await dbContext.Leaderships
                .Where(l => l.KurinKey == kurinKey || (l.GroupKey != null && groupKeys.Contains(l.GroupKey.Value)))
                .ToListAsync();
            dbContext.Leaderships.RemoveRange(leaderships);

            var mentorAssignments = await dbContext.MentorAssignments
                .Where(a => groupKeys.Contains(a.GroupKey))
                .ToListAsync();
            dbContext.MentorAssignments.RemoveRange(mentorAssignments);

            var members = await dbContext.Members
                .Where(m => m.KurinKey == kurinKey)
                .ToListAsync();
            dbContext.Members.RemoveRange(members);

            var groups = await dbContext.Groups
                .Where(g => g.KurinKey == kurinKey)
                .ToListAsync();
            dbContext.Groups.RemoveRange(groups);

            dbContext.Kurins.Remove(kurin1);

            await dbContext.SaveChangesAsync();
        }

        private static async Task<AppUser?> EnsurePasswordlessUser(UserManager<AppUser> userManager, string email, string firstName, string lastName, UserRole role, Guid? kurinKey = null)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = firstName,
                    LastName = lastName,
                    KurinKey = kurinKey,
                    OnboardingStatus = OnboardingStatus.Active
                };

                // Create user without a password
                var result = await userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create passwordless user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                await userManager.AddToRoleAsync(user, role.ToClaimValue());
            }
            else if (user.KurinKey != kurinKey)
            {
                user.KurinKey = kurinKey;
                await userManager.UpdateAsync(user);
            }
            return user;
        }

        /// <summary>
        /// The password every seeded demo/fixture account gets. Seeded accounts exist only in
        /// non-production environments.
        /// </summary>
        internal const string SeededPassword = "User@12345";

        /// <summary>Finds the named group in the kurin, creating it when it is missing.</summary>
        internal static async Task<Group> EnsureGroupAsync(
            AppDbContext dbContext,
            string name,
            Guid kurinKey,
            CancellationToken cancellationToken = default)
        {
            var group = await dbContext.Groups
                .FirstOrDefaultAsync(g => g.Name == name && g.KurinKey == kurinKey, cancellationToken);
            if (group == null)
            {
                group = new Group(name, kurinKey);
                dbContext.Groups.Add(group);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return group;
        }

        /// <summary>
        /// Finds the member by email, creating the member row and its linked account when missing.
        /// Keyed on email so re-running a seeder does not give one account a second member row.
        /// </summary>
        internal static async Task<Member> EnsureMemberAsync(
            AppDbContext dbContext,
            UserManager<AppUser> userManager,
            string email,
            string firstName,
            string lastName,
            Guid kurinKey,
            Guid? groupKey,
            string phoneNumber,
            DateOnly dateOfBirth,
            CancellationToken cancellationToken = default)
        {
            var user = await EnsureUser(userManager, email, firstName, lastName, UserRole.Member, SeededPassword, kurinKey);

            var member = await dbContext.Members.FirstOrDefaultAsync(m => m.Email == email, cancellationToken);
            if (member == null)
            {
                member = new Member
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    DateOfBirth = dateOfBirth,
                    KurinKey = kurinKey,
                    GroupKey = groupKey,
                    UserKey = user!.Id
                };
                dbContext.Members.Add(member);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return member;
        }

        /// <summary>Seats the member in the office unless they already hold it.</summary>
        internal static void AddOffice(Leadership leadership, Guid memberKey, LeadershipRole role)
        {
            var alreadyHolds = leadership.LeadershipHistories
                .Any(h => h.MemberKey == memberKey && h.Role == role && h.EndDate == null);
            if (!alreadyHolds)
            {
                leadership.LeadershipHistories.Add(new LeadershipHistory
                {
                    MemberKey = memberKey,
                    Role = role,
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow)
                });
            }
        }

        internal static async Task<AppUser?> EnsureUser(UserManager<AppUser> userManager, string email, string firstName, string lastName, UserRole role, string password, Guid? kurinKey = null)
        {
            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new AppUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = firstName,
                    LastName = lastName,
                    KurinKey = kurinKey,
                    OnboardingStatus = OnboardingStatus.Active
                };

                var result = await userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }

                await userManager.AddToRoleAsync(user, role.ToClaimValue());
            }
            else if (user.KurinKey != kurinKey)
            {
                user.KurinKey = kurinKey;
                await userManager.UpdateAsync(user);
            }
            return user;
        }
    }
}