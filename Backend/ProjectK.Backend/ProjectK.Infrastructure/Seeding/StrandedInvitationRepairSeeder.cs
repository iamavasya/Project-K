using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Seeding;

/// <summary>
/// One-off, idempotent repair for accounts that were left unable to be invited again.
/// <para>
/// Retention used to delete an approved queue entry thirty days after approval, and the
/// invitation foreign key cascades — so an account that had not activated yet lost both the
/// entry and every invitation hanging off it. Such a person could not activate (their link was
/// gone), could not ask for a new invitation (nothing to hang one off), did not appear in the
/// waitlist panel, and could not be approved again (their address was already taken).
/// </para>
/// <para>
/// This rebuilds the missing entry from the account itself. It issues no invitation and sends no
/// letter: what it restores is the ability to ask for one. Safe on every startup — it no-ops
/// once no account is stranded.
/// </para>
/// </summary>
public static class StrandedInvitationRepairSeeder
{
    public static async Task RepairAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(nameof(StrandedInvitationRepairSeeder));

        var stranded = await dbContext.Users
            .Where(user =>
                user.OnboardingStatus == OnboardingStatus.PendingActivation
                && user.Email != null
                && !dbContext.WaitlistEntries.Any(entry => entry.Email == user.Email))
            .ToListAsync();

        if (stranded.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var user in stranded)
        {
            dbContext.WaitlistEntries.Add(new WaitlistEntry
            {
                WaitlistEntryKey = Guid.NewGuid(),
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber ?? string.Empty,
                // The account never carried one, and the entry demands a value. A date this
                // obviously wrong reads as "unknown" to whoever opens the panel.
                DateOfBirth = DateTime.UnixEpoch,
                IsKurinLeaderCandidate = false,
                VerificationStatus = WaitlistVerificationStatus.ApprovedForInvitation,
                IsBetaParticipant = user.IsBetaParticipant,
                RequestedAtUtc = now,
                ReviewedAtUtc = now,
                ApprovedAtUtc = now
            });
        }

        await dbContext.SaveChangesAsync();

        // Logged loudly: the count is how many people the old retention rule had shut out.
        logger?.LogWarning(
            "Rebuilt {Count} waitlist entries for accounts awaiting activation that had none; they can be invited again.",
            stranded.Count);
    }
}
