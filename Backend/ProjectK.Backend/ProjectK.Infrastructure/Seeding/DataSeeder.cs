using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Seeding;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        // 1. Seed the two roles the identity store still holds. Office roles are not among them:
        // what an office grants is worked out from the registry for the kurin the account is in,
        // and storing it on the account is what used to make it true everywhere at once.
        foreach (var roleName in new[] { SystemRole.Admin, SystemRole.Member })
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new AppRole(roleName));
            }
        }

        // 2. Demo accounts exist only where demo data does, and demo data exists only on the two
        // tiers that are meant to start from the same point every time: Development and E2E.
        // The well-known administrator used to be created in production too, with the password
        // that sits in this file; a deployed instance now gets its first administrator through the
        // setup wizard instead, and this repository holds no password that opens one.
        //
        // Staging and Tailscale used to be on the demo list as well, and it cost real data: the
        // reset below wipes kurin 1 outright, so whatever people entered on a stand shown to
        // them was gone at the next `dev.sh up`, and every account that stood in that kurin was
        // left pointing at a key that no longer existed (STAB-07, STAB-05). Those tiers now keep
        // what they have and get their first administrator the way production does.
        var seedsDemoData = env.IsDevelopment() || env.EnvironmentName == "E2E";
        if (!seedsDemoData)
        {
            await WarnWhenNoAdministratorAsync(scope.ServiceProvider, userManager);
            return;
        }

        await EnsureUser(userManager, "admin@projectk.com", "System", "Admin", UserRole.Admin, "Admin@12345");

        // 3. The load-test account: passwordless, reachable only through LoadTestLoginKey.
        await EnsurePasswordlessUser(userManager, "loadtest@projectk.com", "Load", "Tester", UserRole.Member);

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

    /// <summary>
    /// Said out loud at startup, because a deployment with no administrator is one nobody can
    /// manage until somebody opens the setup wizard — and the log is where that is first noticed.
    /// </summary>
    private static async Task WarnWhenNoAdministratorAsync(IServiceProvider services, UserManager<AppUser> userManager)
    {
        var administrators = await userManager.GetUsersInRoleAsync(SystemRole.Admin);
        if (administrators.Count > 0)
        {
            return;
        }

        services.GetService<ILoggerFactory>()
            ?.CreateLogger(nameof(DataSeeder))
            .LogWarning("No administrator account exists yet; the first visitor is sent to the setup wizard to create one.");
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

        var memberKeys = await dbContext.Memberships
            .Where(ms => ms.KurinKey == kurinKey)
            .Select(ms => ms.MemberKey)
            .Distinct()
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

        // The seeder wipes its own demo kurin outright, people included — the reset exists to
        // give every run the same starting point, and these are not real people.
        var memberships = await dbContext.Memberships
            .Where(ms => ms.KurinKey == kurinKey)
            .ToListAsync();
        dbContext.Memberships.RemoveRange(memberships);

        var members = await dbContext.Members
            .Where(m => memberKeys.Contains(m.MemberKey))
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
                UserKey = user!.Id
            };
            dbContext.Members.Add(member);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        // Seeded people are written straight to the tables, so nothing announces where they were
        // put. Without this row they exist and belong to no kurin, which is to say they are in
        // no list and in nobody's reach.
        var alreadyPlaced = await dbContext.Memberships.AnyAsync(
            ms => ms.MemberKey == member.MemberKey && ms.LeftAtUtc == null,
            cancellationToken);
        if (!alreadyPlaced)
        {
            dbContext.Memberships.Add(new Membership
            {
                MemberKey = member.MemberKey,
                UserKey = member.UserKey,
                KurinKey = kurinKey,
                GroupKey = groupKey,
                Kind = MembershipKind.Youth,
                JoinedAtUtc = DateTime.UtcNow
            });
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
