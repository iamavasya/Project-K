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

            // Manager of Kurin 1
            var manager = await EnsureUser(userManager, "manager1@projectk.com", "Kurin", "Manager", UserRole.Manager, password, kurin1.KurinKey);

            // Mentor of Group 1
            var mentor = await EnsureUser(userManager, "mentor1@projectk.com", "Group", "Mentor", UserRole.Mentor, password, kurin1.KurinKey);
            if (mentor != null && group1 != null)
            {
                var existingAssignment = await dbContext.MentorAssignments.AnyAsync(a => a.MentorUserKey == mentor.Id && a.GroupKey == group1.GroupKey);
                if (!existingAssignment)
                {
                    dbContext.MentorAssignments.Add(new MentorAssignment
                    {
                        MentorUserKey = mentor.Id,
                        GroupKey = group1.GroupKey,
                        AssignedAtUtc = DateTime.UtcNow
                    });
                    await dbContext.SaveChangesAsync();
                }
            }

            // Regular User 1 (linked to Member 1 in Group 1)
            var user1 = await EnsureUser(userManager, "user1@projectk.com", "Member", "One", UserRole.User, password, kurin1.KurinKey);
            if (user1 != null && kurin1 != null && group1 != null)
            {
                var member1 = await dbContext.Members.FirstOrDefaultAsync(m => m.UserKey == user1.Id);
                if (member1 == null)
                {
                    member1 = new Member
                    {
                        FirstName = "Member",
                        LastName = "One",
                        Email = user1.Email!,
                        PhoneNumber = "123456789",
                        DateOfBirth = new DateOnly(2010, 1, 1),
                        KurinKey = kurin1.KurinKey,
                        GroupKey = group1.GroupKey,
                        UserKey = user1.Id
                    };
                    dbContext.Members.Add(member1);
                    await dbContext.SaveChangesAsync();
                }
            }

            // Regular User 2 (linked to Member 2 in Group 2)
            var user2 = await EnsureUser(userManager, "user2@projectk.com", "Member", "Two", UserRole.User, password, kurin1.KurinKey);
            if (user2 != null && kurin1 != null && group2 != null)
            {
                var member2 = await dbContext.Members.FirstOrDefaultAsync(m => m.UserKey == user2.Id);
                if (member2 == null)
                {
                    member2 = new Member
                    {
                        FirstName = "Member",
                        LastName = "Two",
                        Email = user2.Email!,
                        PhoneNumber = "987654321",
                        DateOfBirth = new DateOnly(2011, 2, 2),
                        KurinKey = kurin1.KurinKey,
                        GroupKey = group2.GroupKey,
                        UserKey = user2.Id
                    };
                    dbContext.Members.Add(member2);
                    await dbContext.SaveChangesAsync();
                }
            }

            // Member 3 in Group 1 (NO User account)
            if (kurin1 != null && group1 != null)
            {
                var member3 = await dbContext.Members.FirstOrDefaultAsync(m => m.Email == "member3@no-user.com");
                if (member3 == null)
                {
                    member3 = new Member
                    {
                        FirstName = "Member",
                        LastName = "Three",
                        Email = "member3@no-user.com",
                        PhoneNumber = "555555555",
                        DateOfBirth = new DateOnly(2012, 3, 3),
                        KurinKey = kurin1.KurinKey,
                        GroupKey = group1.GroupKey
                    };
                    dbContext.Members.Add(member3);
                    await dbContext.SaveChangesAsync();
                }
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