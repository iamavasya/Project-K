using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;

public sealed record BadgeProgressAuditEventResponse(
    Guid BadgeProgressAuditEventKey,
    BadgeProgressStatus? FromStatus,
    BadgeProgressStatus ToStatus,
    string Action,
    Guid? ActorUserKey,
    string? ActorName,
    string ActorRole,
    DateTime OccurredAtUtc,
    string? Note);
