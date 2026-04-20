using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Infrastructure.DbContexts;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.API.Services
{
    public class AuditCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<AuditCleanupBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24);

        public AuditCleanupBackgroundService(IServiceProvider serviceProvider, ILogger<AuditCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Audit Cleanup Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupOldRecordsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing Audit Cleanup Service.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Audit Cleanup Service is stopping.");
        }

        private async Task CleanupOldRecordsAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;
            var progressRetentionDate = now.AddDays(-180);
            var onboardingRetentionDate = now.AddDays(-30);

            _logger.LogInformation("Starting database cleanup for records older than retention policies.");

            // 1. Cleanup Badge Progress Audit Events (> 180 days)
            var deletedBadgeAuditEvents = await dbContext.BadgeProgressAuditEvents
                .Where(e => e.OccurredAtUtc < progressRetentionDate)
                .ExecuteDeleteAsync(cancellationToken);
            
            if (deletedBadgeAuditEvents > 0)
                _logger.LogInformation("Cleaned up {Count} old BadgeProgressAuditEvents.", deletedBadgeAuditEvents);

            // 2. Cleanup Probe Progress Audit Events (> 180 days)
            var deletedProbeAuditEvents = await dbContext.ProbeProgressAuditEvents
                .Where(e => e.OccurredAtUtc < progressRetentionDate)
                .ExecuteDeleteAsync(cancellationToken);
            
            if (deletedProbeAuditEvents > 0)
                _logger.LogInformation("Cleaned up {Count} old ProbeProgressAuditEvents.", deletedProbeAuditEvents);

            // 3. Cleanup Waitlist Entries (Terminal status and > 30 days)
            var deletedWaitlistEntries = await dbContext.WaitlistEntries
                .Where(e => 
                    (e.VerificationStatus == WaitlistVerificationStatus.Rejected || 
                     e.VerificationStatus == WaitlistVerificationStatus.ApprovedForInvitation) &&
                    (e.ReviewedAtUtc < onboardingRetentionDate || e.ApprovedAtUtc < onboardingRetentionDate))
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedWaitlistEntries > 0)
                _logger.LogInformation("Cleaned up {Count} old WaitlistEntries.", deletedWaitlistEntries);

            // 4. Cleanup expired/used Invitations (> 30 days)
            var deletedInvitations = await dbContext.Invitations
                .Where(i => 
                    (i.UsedAtUtc.HasValue && i.UsedAtUtc < onboardingRetentionDate) ||
                    (!i.UsedAtUtc.HasValue && i.ExpiresAtUtc < onboardingRetentionDate))
                .ExecuteDeleteAsync(cancellationToken);

            if (deletedInvitations > 0)
                _logger.LogInformation("Cleaned up {Count} old Invitations.", deletedInvitations);
        }
    }
}
