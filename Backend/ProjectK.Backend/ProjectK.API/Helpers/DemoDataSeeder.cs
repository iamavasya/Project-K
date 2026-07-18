using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Extensions;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.API.Helpers
{
    public class DemoDataSeeder : IDemoDataSeeder
    {
        private readonly AppDbContext _dbContext;
        private readonly UserManager<AppUser> _userManager;

        public DemoDataSeeder(AppDbContext dbContext, UserManager<AppUser> userManager)
        {
            _dbContext = dbContext;
            _userManager = userManager;
        }

        public async Task SeedAsync(CancellationToken cancellationToken = default)
        {
            // 1. Seed Kurin
            var kurin1 = await _dbContext.Kurins.FirstOrDefaultAsync(k => k.Number == 1, cancellationToken);
            if (kurin1 == null)
            {
                kurin1 = new Kurin(1) { IsZbtKurin = true };
                _dbContext.Kurins.Add(kurin1);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // 2. Seed Groups
            var group1 = await _dbContext.Groups.FirstOrDefaultAsync(g => g.Name == "Gurtok 1" && g.KurinKey == kurin1.KurinKey, cancellationToken);
            if (group1 == null)
            {
                group1 = new Group("Gurtok 1", kurin1.KurinKey);
                _dbContext.Groups.Add(group1);
            }

            var group2 = await _dbContext.Groups.FirstOrDefaultAsync(g => g.Name == "Gurtok 2" && g.KurinKey == kurin1.KurinKey, cancellationToken);
            if (group2 == null)
            {
                group2 = new Group("Gurtok 2", kurin1.KurinKey);
                _dbContext.Groups.Add(group2);
            }
            await _dbContext.SaveChangesAsync(cancellationToken);

            // 3. Seed Demo Users
            var password = "User@12345";

            // Manager of Kurin 1 (also a member)
            var manager = await DataSeeder.EnsureUser(_userManager, "manager1@projectk.com", "Kurin", "Manager", UserRole.Manager, password, kurin1.KurinKey);
            if (manager != null)
            {
                await EnsureMember(manager, kurin1.KurinKey, null, "Kurin", "Manager", "100000001", new DateOnly(1990, 1, 1));
            }

            // Mentors of groups (each mentor has both user and member)
            var mentor1 = await DataSeeder.EnsureUser(_userManager, "mentor1@projectk.com", "Group", "Mentor", UserRole.Mentor, password, kurin1.KurinKey);
            if (mentor1 != null && group1 != null)
            {
                await EnsureMember(mentor1, kurin1.KurinKey, group1.GroupKey, "Group", "Mentor", "100000002", new DateOnly(1992, 2, 2));
                await EnsureMentorAssignment(mentor1.Id, group1.GroupKey);
            }

            var mentor2 = await DataSeeder.EnsureUser(_userManager, "mentor2@projectk.com", "Second", "Mentor", UserRole.Mentor, password, kurin1.KurinKey);
            if (mentor2 != null && group2 != null)
            {
                await EnsureMember(mentor2, kurin1.KurinKey, group2.GroupKey, "Second", "Mentor", "100000003", new DateOnly(1993, 3, 3));
                await EnsureMentorAssignment(mentor2.Id, group2.GroupKey);
            }

            // Additional members (2 per group)
            if (group1 != null)
            {
                var group1Member1 = await DataSeeder.EnsureUser(_userManager, "g1member1@projectk.com", "Group1", "MemberOne", UserRole.User, password, kurin1.KurinKey);
                if (group1Member1 != null)
                {
                    await EnsureMember(group1Member1, kurin1.KurinKey, group1.GroupKey, "Group1", "MemberOne", "100000011", new DateOnly(2010, 5, 5));
                }

                var group1Member2 = await DataSeeder.EnsureUser(_userManager, "g1member2@projectk.com", "Group1", "MemberTwo", UserRole.User, password, kurin1.KurinKey);
                if (group1Member2 != null)
                {
                    await EnsureMember(group1Member2, kurin1.KurinKey, group1.GroupKey, "Group1", "MemberTwo", "100000012", new DateOnly(2011, 6, 6));
                }
            }

            if (group2 != null)
            {
                var group2Member1 = await DataSeeder.EnsureUser(_userManager, "g2member1@projectk.com", "Group2", "MemberOne", UserRole.User, password, kurin1.KurinKey);
                if (group2Member1 != null)
                {
                    await EnsureMember(group2Member1, kurin1.KurinKey, group2.GroupKey, "Group2", "MemberOne", "100000021", new DateOnly(2010, 7, 7));
                }

                var group2Member2 = await DataSeeder.EnsureUser(_userManager, "g2member2@projectk.com", "Group2", "MemberTwo", UserRole.User, password, kurin1.KurinKey);
                if (group2Member2 != null)
                {
                    await EnsureMember(group2Member2, kurin1.KurinKey, group2.GroupKey, "Group2", "MemberTwo", "100000022", new DateOnly(2011, 8, 8));
                }
            }
        }

        private async Task EnsureMember(
            AppUser user,
            Guid kurinKey,
            Guid? groupKey,
            string firstName,
            string lastName,
            string phoneNumber,
            DateOnly dateOfBirth)
        {
            var isMentor = await _userManager.IsInRoleAsync(user, UserRole.Mentor.ToClaimValue());
            var effectiveGroupKey = isMentor ? null : groupKey;
            var member = await _dbContext.Members.FirstOrDefaultAsync(m => m.UserKey == user.Id);
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
                _dbContext.Members.Add(member);
            }
            else
            {
                member.KurinKey = kurinKey;
                member.GroupKey = effectiveGroupKey;
                member.Email = user.Email!;
            }

            await _dbContext.SaveChangesAsync();
        }

        private async Task EnsureMentorAssignment(Guid mentorUserKey, Guid groupKey)
        {
            var existingAssignment = await _dbContext.MentorAssignments.AnyAsync(
                a => a.MentorUserKey == mentorUserKey && a.GroupKey == groupKey);
            if (!existingAssignment)
            {
                _dbContext.MentorAssignments.Add(new MentorAssignment
                {
                    MentorUserKey = mentorUserKey,
                    GroupKey = groupKey,
                    AssignedAtUtc = DateTime.UtcNow
                });
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
