using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using ProjectK.API.Services;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Infrastructure.DbContexts;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace ProjectK.API.Tests.Services
{
    public class AuditCleanupBackgroundServiceTests : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<AppDbContext> _options;

        public AuditCleanupBackgroundServiceTests()
        {
            _connection = new SqliteConnection("DataSource=:memory:;Foreign Keys=False");
            _connection.Open();

            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = new AppDbContext(_options);
            context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            _connection.Close();
        }

        [Fact]
        public async Task CleanupOldRecordsAsync_ShouldRemoveOldRecordsAndKeepNewOnes()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(opt => opt.UseSqlite(_connection));
            var serviceProvider = services.BuildServiceProvider();

            var loggerMock = new Mock<ILogger<AuditCleanupBackgroundService>>();
            var service = new AuditCleanupBackgroundService(serviceProvider, loggerMock.Object);

            var oldDateProgress = DateTime.UtcNow.AddDays(-181);
            var newDateProgress = DateTime.UtcNow.AddDays(-179);

            var oldDateOnboarding = DateTime.UtcNow.AddDays(-31);
            var newDateOnboarding = DateTime.UtcNow.AddDays(-29);

            using (var context = new AppDbContext(_options))
            {
                // Note: BadgeProgress requires a related BadgeProgress entity, but we only need the audit event for the test if FKs allow, 
                // but SQLite will enforce FKs. So we create dummy parents or disable FKs.
                // SQLite in-memory PRAGMA foreign_keys = OFF is default unless enabled. We'll see.

                context.BadgeProgressAuditEvents.AddRange(
                    new BadgeProgressAuditEvent { BadgeProgressAuditEventKey = Guid.NewGuid(), OccurredAtUtc = oldDateProgress },
                    new BadgeProgressAuditEvent { BadgeProgressAuditEventKey = Guid.NewGuid(), OccurredAtUtc = newDateProgress }
                );

                context.ProbeProgressAuditEvents.AddRange(
                    new ProbeProgressAuditEvent { ProbeProgressAuditEventKey = Guid.NewGuid(), OccurredAtUtc = oldDateProgress },
                    new ProbeProgressAuditEvent { ProbeProgressAuditEventKey = Guid.NewGuid(), OccurredAtUtc = newDateProgress }
                );

                context.WaitlistEntries.AddRange(
                    new WaitlistEntry
                    {
                        WaitlistEntryKey = Guid.NewGuid(),
                        FirstName = "Old",
                        LastName = "User",
                        Email = "old@example.com",
                        PhoneNumber = "1234567890",
                        DateOfBirth = oldDateOnboarding,
                        VerificationStatus = WaitlistVerificationStatus.Rejected,
                        ReviewedAtUtc = oldDateOnboarding
                    },
                    new WaitlistEntry
                    {
                        WaitlistEntryKey = Guid.NewGuid(),
                        FirstName = "New",
                        LastName = "User",
                        Email = "new@example.com",
                        PhoneNumber = "0987654321",
                        DateOfBirth = newDateOnboarding,
                        VerificationStatus = WaitlistVerificationStatus.Rejected,
                        ReviewedAtUtc = newDateOnboarding
                    }
                );

                context.Invitations.AddRange(
                    new Invitation { InvitationKey = Guid.NewGuid(), Token = "old", ExpiresAtUtc = oldDateOnboarding },
                    new Invitation { InvitationKey = Guid.NewGuid(), Token = "new", ExpiresAtUtc = newDateOnboarding }
                );

                await context.SaveChangesAsync();
            }

            // Act
            // We use reflection to call the private method for testing, or we just run the service briefly.
            // Since ExecuteAsync is protected and loops, we can expose the logic or use a reflection workaround.
            var method = typeof(AuditCleanupBackgroundService).GetMethod("CleanupOldRecordsAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method!.Invoke(service, new object[] { CancellationToken.None })!;

            // Assert
            using (var context = new AppDbContext(_options))
            {
                Assert.Equal(1, await context.BadgeProgressAuditEvents.CountAsync());
                Assert.Equal(1, await context.ProbeProgressAuditEvents.CountAsync());
                Assert.Equal(1, await context.WaitlistEntries.CountAsync());
                Assert.Equal(1, await context.Invitations.CountAsync());
            }
        }
    }
}
