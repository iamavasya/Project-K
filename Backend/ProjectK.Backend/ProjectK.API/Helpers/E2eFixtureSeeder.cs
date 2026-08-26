using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectK.BusinessLogic.Modules.AuthModule.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.API.Helpers
{
    /// <summary>
    /// Seeds the stable fixtures the Playwright suite addresses by name: a dedicated kurin holding
    /// "Gurtok 1"/"Gurtok 2" plus the manager1/mentor1/g1member1 accounts.
    /// <para>
    /// Lives in its own kurin so it never collides with <see cref="DemoDataSeeder"/>: the e2e manager
    /// needs the Зв'язковий office (the only one granting whole-kurin management), and that office is
    /// single-holder, so it cannot be added to the demo kurin which already has one.
    /// </para>
    /// </summary>
    public static class E2eFixtureSeeder
    {
        private const int KurinNumber = 2;
        /// <summary>Every fixture body shares one birthday; the suite never asserts on it.</summary>
        private static readonly DateOnly SeededDateOfBirth = new(2004, 1, 1);

        private const string AssignedGroupName = "Gurtok 1";
        private const string UnassignedGroupName = "Gurtok 2";

        public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
        {
            var dbContext = services.GetRequiredService<AppDbContext>();
            var userManager = services.GetRequiredService<UserManager<AppUser>>();
            var roleSync = services.GetRequiredService<ILeadershipRoleSyncService>();

            var kurin = await dbContext.Kurins.FirstOrDefaultAsync(k => k.Number == KurinNumber, cancellationToken);
            if (kurin == null)
            {
                kurin = new Kurin(KurinNumber);
                dbContext.Kurins.Add(kurin);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            var assignedGroup = await DataSeeder.EnsureGroupAsync(dbContext, AssignedGroupName, kurin.KurinKey, cancellationToken);
            await DataSeeder.EnsureGroupAsync(dbContext, UnassignedGroupName, kurin.KurinKey, cancellationToken);

            var kvLeadership = await EnsureKvLeadershipAsync(dbContext, kurin.KurinKey, cancellationToken);

            var manager = await DataSeeder.EnsureMemberAsync(
                dbContext, userManager, "manager1@projectk.com", "Kurin", "Manager", kurin.KurinKey, null, "0500000101", SeededDateOfBirth, cancellationToken);
            DataSeeder.AddOffice(kvLeadership, manager.MemberKey, LeadershipRole.Zvyazkovyi);

            var mentor = await DataSeeder.EnsureMemberAsync(
                dbContext, userManager, "mentor1@projectk.com", "Group", "Mentor", kurin.KurinKey, null, "0500000102", SeededDateOfBirth, cancellationToken);
            DataSeeder.AddOffice(kvLeadership, mentor.MemberKey, LeadershipRole.Vykhovnyk);

            var member = await DataSeeder.EnsureMemberAsync(
                dbContext, userManager, "g1member1@projectk.com", "Group1", "MemberOne", kurin.KurinKey, assignedGroup.GroupKey, "0500000103", SeededDateOfBirth, cancellationToken);

            // The suite needs a second body in Gurtok 1 to assert a member cannot edit somebody else.
            var otherMember = await DataSeeder.EnsureMemberAsync(
                dbContext, userManager, "g1member2@projectk.com", "Group1", "MemberTwo", kurin.KurinKey, assignedGroup.GroupKey, "0500000104", SeededDateOfBirth, cancellationToken);

            await dbContext.SaveChangesAsync(cancellationToken);

            // Scopes the mentor to Gurtok 1 only; the suite asserts they are refused on Gurtok 2.
            var mentorUserKey = mentor.UserKey!.Value;
            var alreadyAssigned = await dbContext.MentorAssignments
                .AnyAsync(a => a.MentorUserKey == mentorUserKey && a.GroupKey == assignedGroup.GroupKey, cancellationToken);
            if (!alreadyAssigned)
            {
                dbContext.MentorAssignments.Add(new MentorAssignment
                {
                    MentorUserKey = mentorUserKey,
                    GroupKey = assignedGroup.GroupKey,
                    AssignedAtUtc = DateTime.UtcNow
                });
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            await roleSync.SyncMembersAsync(
                new HashSet<Guid> { manager.MemberKey, mentor.MemberKey, member.MemberKey, otherMember.MemberKey }, cancellationToken);
        }

        private static async Task<Leadership> EnsureKvLeadershipAsync(
            AppDbContext dbContext, Guid kurinKey, CancellationToken cancellationToken)
        {
            var leadership = await dbContext.Leaderships
                .Include(l => l.LeadershipHistories)
                .FirstOrDefaultAsync(l => l.Type == LeadershipType.KV && l.KurinKey == kurinKey, cancellationToken);

            if (leadership == null)
            {
                leadership = new Leadership
                {
                    Type = LeadershipType.KV,
                    KurinKey = kurinKey,
                    StartDate = DateOnly.FromDateTime(DateTime.UtcNow)
                };
                dbContext.Leaderships.Add(leadership);
                await dbContext.SaveChangesAsync(cancellationToken);
            }

            return leadership;
        }

    }
}
