using ProjectK.Common.Interfaces;

namespace ProjectK.Common.Models.Events;

/// <summary>
/// A person was moved from one гурток of a kurin to another, or into or out of one. Carries the old
/// гурток because the membership no longer remembers it.
/// </summary>
public sealed record MembershipMovedToGroup(
    Guid MembershipKey,
    Guid KurinKey,
    Guid? FromGroupKey,
    Guid? ToGroupKey) : IDomainEvent;

/// <summary>
/// A person's membership in a kurin was closed: released by its Звʼязковий. Whoever counts time in the
/// kurin (the dues ledger) settles the quarter they left in.
/// </summary>
public sealed record MembershipLeft(
    Guid MembershipKey,
    Guid KurinKey,
    DateTime LeftAtUtc) : IDomainEvent;
