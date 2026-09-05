namespace ProjectK.Common.Models.Records;

/// <summary>
/// The rules both onboarding paths share. The lifetime lives here rather than in either handler
/// because the invitation email quotes it too, and the two drifted apart once already.
/// </summary>
public static class OnboardingPolicy
{
    /// <summary>How long an invitation stays usable after it is issued.</summary>
    public const int InvitationLifetimeDays = 7;
}

/// <summary>What a caller must know about an address before it offers an account for it.</summary>
public enum AccountAvailability
{
    /// <summary>No account and no pending waitlist entry hold this address.</summary>
    Available,

    /// <summary>An account already exists for this address.</summary>
    EmailTaken,

    /// <summary>A waitlist entry is already queued for this address; approving it is the way in.</summary>
    WaitlistPending
}

/// <summary>
/// The account to issue. <paramref name="WaitlistEntryKey"/> is the entry the invitation hangs off —
/// an existing one for an approved registration, or the one the caller just created for a member.
/// </summary>
public sealed record AccountProvisioningRequest(
    string Email,
    string FirstName,
    string LastName,
    Guid WaitlistEntryKey,
    Guid? KurinKey,
    bool IsBetaParticipant);

/// <summary>The account that was created and the invitation that lets its owner claim it.</summary>
public sealed record AccountProvisioningResult(Guid UserKey, Guid InvitationKey, string InvitationToken);
