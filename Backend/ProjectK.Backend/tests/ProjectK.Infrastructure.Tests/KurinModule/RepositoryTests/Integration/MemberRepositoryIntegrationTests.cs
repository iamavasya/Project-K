using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.DbContexts;
using InfraUnitOfWork = ProjectK.Infrastructure.UnitOfWork.UnitOfWork;
using ProjectK.Infrastructure.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using ProjectK.Infrastructure.Repositories.AuthModule;
using ProjectK.Infrastructure.Repositories.KurinModule;
using ProjectK.Infrastructure.Repositories.InfrastructureModule;
using ProjectK.Infrastructure.Repositories.ProbesAndBadgesModule;
using ProjectK.Common.Models.Dtos.KurinModule;

namespace ProjectK.Infrastructure.Tests.KurinModule.RepositoryTests.Integration
{
    public class MemberRepositoryIntegrationTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static Member BuildMember(Group group, Kurin kurin, string firstName = "Ivan", string lastName = "Petrenko", string middle = "I.")
            => new Member
            {
                GroupKey = group.GroupKey,
                KurinKey = kurin.KurinKey,
                FirstName = firstName,
                LastName = lastName,
                MiddleName = middle,
                Email = $"{firstName.ToLower()}@example.com",
                PhoneNumber = "123456",
                DateOfBirth = new DateOnly(2000, 1, 1)
            };

        [Fact]
        public async Task Create_And_GetByKeyAsync_ShouldPersistAndIncludeNavigation()
        {
            using var context = CreateInMemoryDbContext();
            var uow = new InfraUnitOfWork(context);

            var kurin = new Kurin(12);
            uow.Kurins.Create(kurin);
            await uow.SaveChangesAsync();

            var group = new Group("Alpha", kurin.KurinKey);
            uow.Groups.Create(group);
            await uow.SaveChangesAsync();

            var member = BuildMember(group, kurin, "Oleh", "Shevchenko");
            uow.Members.Create(member);
            await uow.SaveChangesAsync();

            var fetched = await uow.Members.GetByKeyAsync(member.MemberKey);

            Assert.NotNull(fetched);
            Assert.Equal(member.MemberKey, fetched!.MemberKey);
            Assert.Equal(member.GroupKey, fetched.GroupKey);
            Assert.Equal(member.KurinKey, fetched.KurinKey);
            Assert.NotNull(fetched.Group);
            Assert.NotNull(fetched.Kurin);
            Assert.Equal(group.Name, fetched.Group.Name);
            Assert.Equal(kurin.Number, fetched.Kurin.Number);
        }

        [Fact]
        public async Task GetAllAsync_ByGroupKey_ShouldReturnOnlyThatGroupMembers()
        {
            using var context = CreateInMemoryDbContext();
            var uow = new InfraUnitOfWork(context);

            var kurin = new Kurin(1);
            uow.Kurins.Create(kurin);
            await uow.SaveChangesAsync();

            var group1 = new Group("G1", kurin.KurinKey);
            var group2 = new Group("G2", kurin.KurinKey);
            uow.Groups.Create(group1);
            uow.Groups.Create(group2);
            await uow.SaveChangesAsync();

            uow.Members.Create(BuildMember(group1, kurin, "A1", "L1"));
            uow.Members.Create(BuildMember(group1, kurin, "A2", "L2"));
            uow.Members.Create(BuildMember(group2, kurin, "B1", "L3"));
            await uow.SaveChangesAsync();

            var group1Members = (await uow.Members.GetAllAsync(group1.GroupKey)).ToList();

            Assert.Equal(2, group1Members.Count);
            Assert.All(group1Members, m => Assert.Equal(group1.GroupKey, m.GroupKey));
            Assert.All(group1Members, m => Assert.NotNull(m.Group));
            Assert.All(group1Members, m => Assert.NotNull(m.Kurin));
        }

        [Fact]
        public async Task GetAllByKurinKeyAsync_ShouldReturnMembersAcrossGroups()
        {
            using var context = CreateInMemoryDbContext();
            var uow = new InfraUnitOfWork(context);

            var kurin1 = new Kurin(10);
            var kurin2 = new Kurin(20);
            uow.Kurins.Create(kurin1);
            uow.Kurins.Create(kurin2);
            await uow.SaveChangesAsync();

            var g1a = new Group("K1-G1", kurin1.KurinKey);
            var g1b = new Group("K1-G2", kurin1.KurinKey);
            var g2a = new Group("K2-G1", kurin2.KurinKey);
            uow.Groups.Create(g1a);
            uow.Groups.Create(g1b);
            uow.Groups.Create(g2a);
            await uow.SaveChangesAsync();

            uow.Members.Create(BuildMember(g1a, kurin1, "M1", "L1"));
            uow.Members.Create(BuildMember(g1b, kurin1, "M2", "L2"));
            uow.Members.Create(BuildMember(g2a, kurin2, "M3", "L3"));
            await uow.SaveChangesAsync();

            var kurin1Members = (await uow.Members.GetAllByKurinKeyAsync(kurin1.KurinKey)).ToList();

            Assert.Equal(2, kurin1Members.Count);
            Assert.All(kurin1Members, m => Assert.Equal(kurin1.KurinKey, m.KurinKey));
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrue_WhenMemberExists()
        {
            using var context = CreateInMemoryDbContext();
            var uow = new InfraUnitOfWork(context);

            var kurin = new Kurin(5);
            uow.Kurins.Create(kurin);
            await uow.SaveChangesAsync();

            var group = new Group("GG", kurin.KurinKey);
            uow.Groups.Create(group);
            await uow.SaveChangesAsync();

            var member = BuildMember(group, kurin, "Exist", "Test");
            uow.Members.Create(member);
            await uow.SaveChangesAsync();

            var exists = await uow.Members.ExistsAsync(member.MemberKey);
            var notExists = await uow.Members.ExistsAsync(Guid.NewGuid());

            Assert.True(exists);
            Assert.False(notExists);
        }

        [Fact]
        public async Task Update_ShouldModifyFields()
        {
            using var context = CreateInMemoryDbContext();
            var uow = new InfraUnitOfWork(context);

            var kurin = new Kurin(2);
            uow.Kurins.Create(kurin);
            await uow.SaveChangesAsync();

            var group = new Group("Alpha", kurin.KurinKey);
            uow.Groups.Create(group);
            await uow.SaveChangesAsync();

            var member = BuildMember(group, kurin, "Old", "Name");
            uow.Members.Create(member);
            await uow.SaveChangesAsync();

            member.FirstName = "New";
            member.PhoneNumber = "999999";
            uow.Members.Update(member);
            await uow.SaveChangesAsync();

            var fetched = await uow.Members.GetByKeyAsync(member.MemberKey);
            Assert.NotNull(fetched);
            Assert.Equal("New", fetched!.FirstName);
            Assert.Equal("999999", fetched.PhoneNumber);
        }

        [Fact]
        public async Task Delete_ShouldRemoveMember()
        {
            using var context = CreateInMemoryDbContext();
            var uow = new InfraUnitOfWork(context);

            var kurin = new Kurin(3);
            uow.Kurins.Create(kurin);
            await uow.SaveChangesAsync();

            var group = new Group("Beta", kurin.KurinKey);
            uow.Groups.Create(group);
            await uow.SaveChangesAsync();

            var member = BuildMember(group, kurin, "Del", "User");
            uow.Members.Create(member);
            await uow.SaveChangesAsync();

            uow.Members.Delete(member);
            await uow.SaveChangesAsync();

            var deleted = await uow.Members.GetByKeyAsync(member.MemberKey);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task GetListItemsByKurinKeyAsync_ShouldReturnOnlyActiveLeadershipAndWarnings()
        {
            using var context = CreateInMemoryDbContext();
            var uow = new InfraUnitOfWork(context);

            var kurin = new Kurin(7);
            uow.Kurins.Create(kurin);
            await uow.SaveChangesAsync();

            var group = new Group("Alpha", kurin.KurinKey);
            uow.Groups.Create(group);
            await uow.SaveChangesAsync();

            var member = BuildMember(group, kurin, "Active", "Roles");
            uow.Members.Create(member);
            await uow.SaveChangesAsync();

            var leadership = new Leadership
            {
                LeadershipKey = Guid.NewGuid(),
                Type = LeadershipType.Group,
                GroupKey = group.GroupKey,
                Group = group,
                Name = "Alpha leadership",
                StartDate = new DateOnly(2024, 1, 1)
            };
            context.Set<Leadership>().Add(leadership);
            context.Set<LeadershipHistory>().AddRange(
                new LeadershipHistory
                {
                    LeadershipHistoryKey = Guid.NewGuid(),
                    MemberKey = member.MemberKey,
                    LeadershipKey = leadership.LeadershipKey,
                    Role = LeadershipRole.Hurtkoviy,
                    StartDate = new DateOnly(2024, 1, 1),
                    EndDate = null // active
                },
                new LeadershipHistory
                {
                    LeadershipHistoryKey = Guid.NewGuid(),
                    MemberKey = member.MemberKey,
                    LeadershipKey = leadership.LeadershipKey,
                    Role = LeadershipRole.Pysar,
                    StartDate = new DateOnly(2022, 1, 1),
                    EndDate = new DateOnly(2023, 1, 1) // archived
                });
            context.Set<MemberWarning>().AddRange(
                new MemberWarning
                {
                    MemberKey = member.MemberKey,
                    Level = MemberWarningLevel.Level2,
                    IssuedAtUtc = DateTime.UtcNow.AddDays(-1),
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(30),
                    IssuedByUserKey = Guid.NewGuid(),
                    RevokedAtUtc = null // active
                },
                new MemberWarning
                {
                    MemberKey = member.MemberKey,
                    Level = MemberWarningLevel.Level1,
                    IssuedAtUtc = DateTime.UtcNow.AddDays(-10),
                    ExpiresAtUtc = DateTime.UtcNow.AddDays(-1),
                    IssuedByUserKey = Guid.NewGuid(),
                    RevokedAtUtc = DateTime.UtcNow.AddDays(-2) // revoked
                });
            await context.SaveChangesAsync();

            var visibility = new MemberFieldVisibility(CanSeeAllPrivate: true, CurrentUserId: null, VisibleGroupKeys: Array.Empty<Guid>());
            var items = (await uow.Members.GetListItemsByKurinKeyAsync(kurin.KurinKey, visibility)).ToList();

            var item = Assert.Single(items);
            var activeLeadership = Assert.Single(item.LeadershipHistories);
            Assert.Equal(LeadershipRole.Hurtkoviy, activeLeadership.Role);
            Assert.Equal(LeadershipType.Group, activeLeadership.LeadershipType);
            Assert.Equal(group.Name, activeLeadership.GroupName);

            var activeWarning = Assert.Single(item.Warnings);
            Assert.Equal(MemberWarningLevel.Level2, activeWarning.Level);
        }

        [Fact]
        public async Task GetListItemsByKurinKeyAsync_ShouldMaskPrivateFields_ByVisibility()
        {
            using var context = CreateInMemoryDbContext();
            var uow = new InfraUnitOfWork(context);

            var kurin = new Kurin(8);
            uow.Kurins.Create(kurin);
            await uow.SaveChangesAsync();

            var ownGroup = new Group("Own", kurin.KurinKey);
            var visibleGroup = new Group("Visible", kurin.KurinKey);
            var hiddenGroup = new Group("Hidden", kurin.KurinKey);
            uow.Groups.Create(ownGroup);
            uow.Groups.Create(visibleGroup);
            uow.Groups.Create(hiddenGroup);
            await uow.SaveChangesAsync();

            var ownerUserKey = Guid.NewGuid();
            var owner = BuildMember(ownGroup, kurin, "Owner", "Self");
            owner.UserKey = ownerUserKey;
            owner.Address = "Owner St";
            owner.School = "Owner School";
            var inVisibleGroup = BuildMember(visibleGroup, kurin, "In", "VisibleGroup");
            inVisibleGroup.Address = "Visible St";
            inVisibleGroup.School = "Visible School";
            var hidden = BuildMember(hiddenGroup, kurin, "In", "HiddenGroup");
            hidden.Address = "Hidden St";
            hidden.School = "Hidden School";
            uow.Members.Create(owner);
            uow.Members.Create(inVisibleGroup);
            uow.Members.Create(hidden);
            await uow.SaveChangesAsync();

            // Caller is a mentor: not admin/manager, owns `owner`'s account, assigned to visibleGroup only.
            var visibility = new MemberFieldVisibility(
                CanSeeAllPrivate: false,
                CurrentUserId: ownerUserKey,
                VisibleGroupKeys: new[] { visibleGroup.GroupKey });

            var items = (await uow.Members.GetListItemsByKurinKeyAsync(kurin.KurinKey, visibility)).ToList();

            var ownerItem = items.Single(i => i.MemberKey == owner.MemberKey);
            var visibleItem = items.Single(i => i.MemberKey == inVisibleGroup.MemberKey);
            var hiddenItem = items.Single(i => i.MemberKey == hidden.MemberKey);

            Assert.Equal("Owner St", ownerItem.Address);           // own record
            Assert.Equal("Visible St", visibleItem.Address);       // assigned group
            Assert.Null(hiddenItem.Address);                       // masked
            Assert.Null(hiddenItem.School);
        }

        [Fact]
        public async Task GetAllAsync_Parameterless_ShouldThrowNotSupported()
        {
            using var context = CreateInMemoryDbContext();
            var repo = new MemberRepository(context);

            await Assert.ThrowsAsync<NotSupportedException>(async () =>
            {
                _ = await repo.GetAllAsync(); // parameterless
            });
        }
    }
}
