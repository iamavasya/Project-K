using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Extensions;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.API.Helpers
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

            // 1. Seed Roles (Required for all environments)
            foreach (var roleName in Enum.GetNames(typeof(UserRole)))
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new AppRole(roleName));
                }
            }

            // 2. Seed System Admin (Required for initial setup)
            await EnsureUser(userManager, "admin@projectk.com", "System", "Admin", UserRole.Admin, "Admin@12345");

            // --- STOP HERE FOR PRODUCTION ---
            // Only seed comprehensive test data in Development or other non-prod environments
            if (env.IsProduction())
            {
                return;
            }

            await ResetKurin1DataAsync(dbContext, userManager);

            // 3. Seed Kurin
            var kurin1 = await dbContext.Kurins.FirstOrDefaultAsync(k => k.Number == 1);
            if (kurin1 == null)
            {
                kurin1 = new Kurin(1) { IsZbtKurin = true };
                dbContext.Kurins.Add(kurin1);
                await dbContext.SaveChangesAsync();
            }

            // 4. Seed Groups
            var group1 = await dbContext.Groups.FirstOrDefaultAsync(g => g.Name == "Gurtok 1" && g.KurinKey == kurin1.KurinKey);
            if (group1 == null)
            {
                group1 = new Group("Gurtok 1", kurin1.KurinKey);
                dbContext.Groups.Add(group1);
            }

            var group2 = await dbContext.Groups.FirstOrDefaultAsync(g => g.Name == "Gurtok 2" && g.KurinKey == kurin1.KurinKey);
            if (group2 == null)
            {
                group2 = new Group("Gurtok 2", kurin1.KurinKey);
                dbContext.Groups.Add(group2);
            }
            await dbContext.SaveChangesAsync();

            // 5. Seed Test Users
            var password = "User@12345";

            // Manager of Kurin 1 (also a member)
            var manager = await EnsureUser(userManager, "manager1@projectk.com", "Kurin", "Manager", UserRole.Manager, password, kurin1.KurinKey);
            if (manager != null)
            {
                await EnsureMember(dbContext, userManager, manager, kurin1.KurinKey, null, "Kurin", "Manager", "100000001", new DateOnly(1990, 1, 1));
            }

            // Mentors of groups (each mentor has both user and member)
            var mentor1 = await EnsureUser(userManager, "mentor1@projectk.com", "Group", "Mentor", UserRole.Mentor, password, kurin1.KurinKey);
            if (mentor1 != null && group1 != null)
            {
                await EnsureMember(dbContext, userManager, mentor1, kurin1.KurinKey, group1.GroupKey, "Group", "Mentor", "100000002", new DateOnly(1992, 2, 2));
                await EnsureMentorAssignment(dbContext, mentor1.Id, group1.GroupKey);
            }

            var mentor2 = await EnsureUser(userManager, "mentor2@projectk.com", "Second", "Mentor", UserRole.Mentor, password, kurin1.KurinKey);
            if (mentor2 != null && group2 != null)
            {
                await EnsureMember(dbContext, userManager, mentor2, kurin1.KurinKey, group2.GroupKey, "Second", "Mentor", "100000003", new DateOnly(1993, 3, 3));
                await EnsureMentorAssignment(dbContext, mentor2.Id, group2.GroupKey);
            }

            // Additional members (2 per group)
            if (group1 != null)
            {
                var group1Member1 = await EnsureUser(userManager, "g1member1@projectk.com", "Group1", "MemberOne", UserRole.User, password, kurin1.KurinKey);
                if (group1Member1 != null)
                {
                    await EnsureMember(dbContext, userManager, group1Member1, kurin1.KurinKey, group1.GroupKey, "Group1", "MemberOne", "100000011", new DateOnly(2010, 5, 5));
                }

                var group1Member2 = await EnsureUser(userManager, "g1member2@projectk.com", "Group1", "MemberTwo", UserRole.User, password, kurin1.KurinKey);
                if (group1Member2 != null)
                {
                    await EnsureMember(dbContext, userManager, group1Member2, kurin1.KurinKey, group1.GroupKey, "Group1", "MemberTwo", "100000012", new DateOnly(2011, 6, 6));
                }
            }

            if (group2 != null)
            {
                var group2Member1 = await EnsureUser(userManager, "g2member1@projectk.com", "Group2", "MemberOne", UserRole.User, password, kurin1.KurinKey);
                if (group2Member1 != null)
                {
                    await EnsureMember(dbContext, userManager, group2Member1, kurin1.KurinKey, group2.GroupKey, "Group2", "MemberOne", "100000021", new DateOnly(2010, 7, 7));
                }

                var group2Member2 = await EnsureUser(userManager, "g2member2@projectk.com", "Group2", "MemberTwo", UserRole.User, password, kurin1.KurinKey);
                if (group2Member2 != null)
                {
                    await EnsureMember(dbContext, userManager, group2Member2, kurin1.KurinKey, group2.GroupKey, "Group2", "MemberTwo", "100000022", new DateOnly(2011, 8, 8));
                }
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

        private static async Task EnsureMember(
            AppDbContext dbContext,
            UserManager<AppUser> userManager,
            AppUser user,
            Guid kurinKey,
            Guid? groupKey,
            string firstName,
            string lastName,
            string phoneNumber,
            DateOnly dateOfBirth)
        {
            var isMentor = await userManager.IsInRoleAsync(user, UserRole.Mentor.ToClaimValue());
            var effectiveGroupKey = isMentor ? null : groupKey;
            var member = await dbContext.Members.FirstOrDefaultAsync(m => m.UserKey == user.Id);
            if (member == null)
            {
                member = new Member
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = user.Email!,
                    PhoneNumber = phoneNumber,
                    DateOfBirth = dateOfBirth,
                    KurinKey = kurinKey,
                    GroupKey = effectiveGroupKey,
                    UserKey = user.Id
                };
                dbContext.Members.Add(member);
            }
            else
            {
                member.KurinKey = kurinKey;
                member.GroupKey = effectiveGroupKey;
                member.Email = user.Email!;
            }

            await dbContext.SaveChangesAsync();
        }

        private static async Task EnsureMentorAssignment(AppDbContext dbContext, Guid mentorUserKey, Guid groupKey)
        {
            var existingAssignment = await dbContext.MentorAssignments.AnyAsync(
                a => a.MentorUserKey == mentorUserKey && a.GroupKey == groupKey);
            if (!existingAssignment)
            {
                dbContext.MentorAssignments.Add(new MentorAssignment
                {
                    MentorUserKey = mentorUserKey,
                    GroupKey = groupKey,
                    AssignedAtUtc = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync();
            }
        }

        private static async Task<AppUser?> EnsureUser(UserManager<AppUser> userManager, string email, string firstName, string lastName, UserRole role, string password, Guid? kurinKey = null)
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
                    throw new Exception($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
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