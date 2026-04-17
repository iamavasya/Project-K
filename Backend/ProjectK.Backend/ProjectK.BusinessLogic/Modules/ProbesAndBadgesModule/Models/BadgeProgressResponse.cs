using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;

public sealed class BadgeProgressResponse
{
    public Guid BadgeProgressKey { get; init; }
    public Guid MemberKey { get; init; }
    public Guid KurinKey { get; init; }
    public string BadgeId { get; init; } = string.Empty;
    public BadgeProgressStatus Status { get; init; }
    public DateTime? SubmittedAtUtc { get; init; }
    public DateTime? ReviewedAtUtc { get; init; }
    public Guid? ReviewedByUserKey { get; init; }
    public string? ReviewedByName { get; init; }
    public string? ReviewedByRole { get; init; }
    public string? ReviewNote { get; init; }
    public IReadOnlyCollection<BadgeProgressAuditEventResponse> AuditTrail { get; init; } = [];

    public static BadgeProgressResponse FromEntity(BadgeProgress entity)
    {
        var auditTrail = entity.AuditEvents
            .OrderBy(x => x.OccurredAtUtc)
            .Select(x => new BadgeProgressAuditEventResponse(
                x.BadgeProgressAuditEventKey,
                x.FromStatus,
                x.ToStatus,
                x.Action,
                x.ActorUserKey,
                x.ActorName,
                x.ActorRole,
                x.OccurredAtUtc,
                x.Note))
            .ToList();

        return new BadgeProgressResponse
        {
            BadgeProgressKey = entity.BadgeProgressKey,
            MemberKey = entity.MemberKey,
            KurinKey = entity.KurinKey,
            BadgeId = entity.BadgeId,
            Status = entity.Status,
            SubmittedAtUtc = entity.SubmittedAtUtc,
            ReviewedAtUtc = entity.ReviewedAtUtc,
            ReviewedByUserKey = entity.ReviewedByUserKey,
            ReviewedByName = entity.ReviewedByName,
            ReviewedByRole = entity.ReviewedByRole,
            ReviewNote = entity.ReviewNote,
            AuditTrail = auditTrail
        };
    }
}
