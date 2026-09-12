namespace ProjectK.Common.Models.Records;

/// <summary>
/// Whether a member exists and which account it is linked to. The account flow needs both before it
/// writes anything, and neither is worth loading the member's whole graph for.
/// </summary>
public sealed record MemberAccountLink(Guid MemberKey, Guid? UserKey);

/// <summary>
/// What the profile write leaves behind for the steps that run after it: the member it wrote, whether
/// it was newly created, and the photo it replaced — the callers of those steps have no other way to
/// know, because each runs as its own use case.
/// </summary>
public sealed record MemberProfileWriteResult(
    Guid MemberKey,
    bool IsCreated,
    bool WasProfileVerifiedCurrent,
    string? PreviousPhotoBlobName);
