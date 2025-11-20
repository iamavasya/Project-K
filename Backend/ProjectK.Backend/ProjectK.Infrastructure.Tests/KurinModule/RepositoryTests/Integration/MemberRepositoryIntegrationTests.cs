using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Infrastructure.DbContexts;
using InfraUnitOfWork = ProjectK.Infrastructure.UnitOfWork.UnitOfWork;
using ProjectK.Infrastructure.Repositories;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

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

        //trigger

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
