using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Infrastructure.DbContexts;
using ProjectK.Infrastructure.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.BusinessLogic.Tests.KurinModule.RepositoryTests.Integration
{
    public class KurinRepositoryIntegrationTests
    {
        private AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public async Task Create_And_GetByKeyAsync_ShouldWorkCorrectly()
        {
            // Arrange
            var kurin = new Kurin(10);
            var kurinKey = kurin.KurinKey;

            using (var context = CreateInMemoryDbContext())
            {
                var unitOfWork = new UnitOfWork(context);

                // Act
                unitOfWork.Kurins.Create(kurin);
                await unitOfWork.SaveChangesAsync();

                // Assert
                var fetched = await unitOfWork.Kurins.GetByKeyAsync(kurinKey);
                Assert.NotNull(fetched);
                Assert.Equal(10, fetched!.Number);
                Assert.Equal(kurinKey, fetched.KurinKey);
            }
        }

        [Fact]
        public async Task Update_ShouldModifyExistingEntity()
        {
            // Arrange
            using (var context = CreateInMemoryDbContext())
            {
                var unitOfWork = new UnitOfWork(context);

                var kurin = new Kurin(5);
                var kurinKey = kurin.KurinKey;
                unitOfWork.Kurins.Create(kurin);
                await unitOfWork.SaveChangesAsync();

                // Act
                kurin.Number = 20;
                unitOfWork.Kurins.Update(kurin);
                await unitOfWork.SaveChangesAsync();

                // Assert
                var updated = await unitOfWork.Kurins.GetByKeyAsync(kurinKey);
                Assert.NotNull(updated);
                Assert.Equal(20, updated!.Number);
            }
        }

        [Fact]
        public async Task Delete_ShouldRemoveEntity()
        {
            // Arrange
            using (var context = CreateInMemoryDbContext())
            {
                var unitOfWork = new UnitOfWork(context);

                var kurin = new Kurin(7);
                var kurinKey = kurin.KurinKey;
                unitOfWork.Kurins.Create(kurin);
                await unitOfWork.SaveChangesAsync();

                // Act
                unitOfWork.Kurins.Delete(kurin);
                await unitOfWork.SaveChangesAsync();

                // Assert
                var deleted = await unitOfWork.Kurins.GetByKeyAsync(kurinKey);
                Assert.Null(deleted);
            }
        }
    }
}
