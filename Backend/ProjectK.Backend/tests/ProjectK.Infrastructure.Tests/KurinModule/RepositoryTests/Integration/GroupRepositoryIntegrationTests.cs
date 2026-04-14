using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Infrastructure.DbContexts;
using InfraUnitOfWork = ProjectK.Infrastructure.UnitOfWork.UnitOfWork;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ProjectK.Infrastructure.Tests.KurinModule.RepositoryTests.Integration
{
    public class GroupRepositoryIntegrationTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task Create_And_GetByKeyAsync_ShouldPersistAndReturnWithNavigation()
        {
            using var context = CreateInMemoryDbContext();
            var uow = new InfraUnitOfWork(context);

            var kurin = new Kurin(11);
            uow.Kurins.Create(kurin);
            await uow.SaveChangesAsync();

            var group = new Group("Alpha", kurin.KurinKey);
            uow.Groups.Create(group);
            await uow.SaveChangesAsync();

            var fetched = await uow.Groups.GetByKeyAsync(group.GroupKey);

            Assert.NotNull(fetched);
            Assert.Equal(group.GroupKey, fetched!.GroupKey);
            Assert.Equal("Alpha", fetched.Name);
            Assert.Equal(kurin.KurinKey, fetched.KurinKey);
            Assert.NotNull(fetched.Kurin);
            Assert.Equal(kurin.Number, fetched.Kurin.Number);
        }

        [Fact]
        public async Task GetAllAsync_ByKurinKey_ShouldReturnOnlyThatKurinGroups()
        {
            using var context = CreateInMemoryDbContext();
            var uow = new InfraUnitOfWork(context);

            var kurin1 = new Kurin(1);
            var kurin2 = new Kurin(2);
            uow.Kurins.Create(kurin1);
            uow.Kurins.Create(kurin2);
            await uow.SaveChangesAsync();

            uow.Groups.Create(new Group("G1", kurin1.KurinKey));
            uow.Groups.Create(new Group("G2", kurin1.KurinKey));
            uow.Groups.Create(new Group("G3", kurin2.KurinKey));
            await uow.SaveChangesAsync();

            var kurin1Groups = (await uow.Groups.GetAllAsync(kurin1.KurinKey)).ToList();

            Assert.Equal(2, kurin1Groups.Count);
            Assert.All(kurin1Groups, g => Assert.Equal(kurin1.KurinKey, g.KurinKey));
            Assert.All(kurin1Groups, g => Assert.NotNull(g.Kurin));
            Assert.All(kurin1Groups, g => Assert.Equal(kurin1.Number, g.Kurin.Number));
        }

        [Fact]
        public async Task GetAllAsync_WhenNoneExist_ShouldReturnEmpty()
        {
            using var context = CreateInMemoryDbContext();
            var uow = new InfraUnitOfWork(context);

            var kurin = new Kurin(5);
            uow.Kurins.Create(kurin);
            await uow.SaveChangesAsync();

            var groups = await uow.Groups.GetAllAsync(kurin.KurinKey);

            Assert.NotNull(groups);
            Assert.Empty(groups);
        }

        [Fact]
        public async Task Update_ShouldModifyGroupName()
        {
            using var context = CreateInMemoryDbContext();
            var uow = new InfraUnitOfWork(context);

            var kurin = new Kurin(9);
            uow.Kurins.Create(kurin);
            await uow.SaveChangesAsync();

            var group = new Group("Old", kurin.KurinKey);
            uow.Groups.Create(group);
            await uow.SaveChangesAsync();

            group.Name = "NewName";
            uow.Groups.Update(group);
            await uow.SaveChangesAsync();

            var fetched = await uow.Groups.GetByKeyAsync(group.GroupKey);
            Assert.NotNull(fetched);
            Assert.Equal("NewName", fetched!.Name);
        }

        [Fact]
        public async Task Delete_ShouldRemoveGroup()
        {
            using var context = CreateInMemoryDbContext();
            var uow = new InfraUnitOfWork(context);

            var kurin = new Kurin(3);
            uow.Kurins.Create(kurin);
            await uow.SaveChangesAsync();

            var group = new Group("ToDelete", kurin.KurinKey);
            uow.Groups.Create(group);
            await uow.SaveChangesAsync();

            uow.Groups.Delete(group);
            await uow.SaveChangesAsync();

            var fetched = await uow.Groups.GetByKeyAsync(group.GroupKey);
            Assert.Null(fetched);
        }

        [Fact]
        public async Task ExistsAsync_ShouldReturnTrueForExistingGroup()
        {
            using var context = CreateInMemoryDbContext();
            var uow = new InfraUnitOfWork(context);

            var kurin = new Kurin(8);
            uow.Kurins.Create(kurin);
            await uow.SaveChangesAsync();

            var group = new Group("Present", kurin.KurinKey);
            uow.Groups.Create(group);
            await uow.SaveChangesAsync();

            var exists = await uow.Groups.ExistsAsync(group.GroupKey);
            Assert.True(exists);
        }

        [Fact]
        public async Task GetAllAsync_Parameterless_ShouldThrowNotSupported()
        {
            using var context = CreateInMemoryDbContext();
            var uow = new InfraUnitOfWork(context);

            await Assert.ThrowsAsync<NotSupportedException>(async () =>
            {
                await uow.Groups.GetAllAsync();
            });
        }
    }
}