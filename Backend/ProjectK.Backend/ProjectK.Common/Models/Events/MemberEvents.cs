using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Events;

/// <summary>
/// A verified profile was edited, so what was checked no longer matches what is stored.
/// </summary>
public sealed record MemberProfileWentStale(
    Guid MemberKey,
    Guid MemberUserKey,
    Guid? ActorUserKey) : IDomainEvent;

/// <summary>A profile was checked against the person and confirmed current.</summary>
public sealed record MemberProfileVerified(
    Guid MemberKey,
    Guid MemberUserKey,
    Guid? ActorUserKey) : IDomainEvent;

/// <summary>An award was put forward and is waiting for the провід to look at it.</summary>
public sealed record MemberAwardSubmitted(
    Guid MemberAwardKey,
    Guid MemberKey,
    string MemberName,
    Guid KurinKey,
    Guid? GroupKey,
    Guid? ActorUserKey) : IDomainEvent;

/// <summary>An award was looked at and either stands or does not.</summary>
public sealed record MemberAwardReviewed(
    Guid MemberAwardKey,
    Guid MemberKey,
    Guid MemberUserKey,
    bool IsApproved,
    Guid? ActorUserKey) : IDomainEvent;

/// <summary>A пересторога was written against a member.</summary>
public sealed record MemberWarningAssigned(
    Guid MemberWarningKey,
    Guid MemberKey,
    Guid MemberUserKey,
    MemberWarningLevel Level,
    Guid? ActorUserKey) : IDomainEvent;

/// <summary>A вмілість was handed in and is waiting to be reviewed.</summary>
public sealed record BadgeProgressSubmitted(
    Guid BadgeProgressKey,
    string BadgeId,
    Guid MemberKey,
    string MemberName,
    Guid KurinKey,
    Guid? GroupKey,
    Guid? ActorUserKey) : IDomainEvent;

/// <summary>
/// A вмілість was reviewed. <paramref name="ConfirmationWithdrawn"/> separates "not accepted yet"
/// from "an earlier confirmation was taken back", which read the same to the member otherwise.
/// </summary>
public sealed record BadgeProgressReviewed(
    Guid BadgeProgressKey,
    string BadgeId,
    Guid MemberKey,
    Guid MemberUserKey,
    bool IsApproved,
    bool ConfirmationWithdrawn,
    Guid? ActorUserKey) : IDomainEvent;

/// <summary>
/// These people are gone from the system. Whoever keeps anything keyed by a member — progress,
/// history, assignments — clears it in response; the module that removed them does not need to know
/// who that is.
/// </summary>
public sealed record MembersRemoved(IReadOnlyCollection<Guid> MemberKeys) : IDomainEvent;
