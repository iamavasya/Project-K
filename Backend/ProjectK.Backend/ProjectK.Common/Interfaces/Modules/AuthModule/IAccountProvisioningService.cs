using ProjectK.Common.Models.Records;

namespace ProjectK.Common.Interfaces.Modules.AuthModule;

/// <summary>
/// Issues the pending account an invited person signs in with: the <c>AppUser</c> and the invitation
/// that carries its token. Both ways in go through here — a провід adding a member and an approved
/// waitlist entry — so the account shape and the invitation lifetime are decided once.
/// <para>
/// The account is persisted immediately (Identity writes through its own store); the invitation is
/// only added to the unit of work, so the caller commits it together with the rest of its use case.
/// </para>
/// </summary>
public interface IAccountProvisioningService
{
    /// <summary>Whether an account can still be issued for this address, and what holds it if not.</summary>
    Task<AccountAvailability> CheckAvailabilityAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the pending account and its invitation. Failure carries the identity store's own
    /// reason, so the caller can decide between reporting it and treating it as a broken invariant.
    /// </summary>
    Task<ServiceResult<AccountProvisioningResult>> ProvisionAsync(
        AccountProvisioningRequest request,
        CancellationToken cancellationToken = default);
}
