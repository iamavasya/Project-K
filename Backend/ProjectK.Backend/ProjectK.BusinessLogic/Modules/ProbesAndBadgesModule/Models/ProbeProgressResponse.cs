using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;

public sealed class ProbeProgressResponse
{
    public Guid? ProbeProgressKey { get; init; }
    public Guid MemberKey { get; init; }
    public Guid KurinKey { get; init; }
    public string ProbeId { get; init; } = string.Empty;
    public ProbeProgressStatus Status { get; init; }
    public DateTime? CompletedAtUtc { get; init; }
    public Guid? CompletedByUserKey { get; init; }
    public string? CompletedByName { get; init; }
    public string? CompletedByRole { get; init; }
    public DateTime? VerifiedAtUtc { get; init; }
    public Guid? VerifiedByUserKey { get; init; }
    public string? VerifiedByName { get; init; }
    public string? VerifiedByRole { get; init; }
    public IReadOnlyCollection<ProbeProgressAuditEventResponse> AuditTrail { get; init; } = [];

    public static ProbeProgressResponse CreateNotStarted(Guid memberKey, Guid kurinKey, string probeId)
    {
        return new ProbeProgressResponse
        {
            ProbeProgressKey = null,
            MemberKey = memberKey,
            KurinKey = kurinKey,
            ProbeId = probeId,
            Status = ProbeProgressStatus.NotStarted,
            AuditTrail = []
        };
    }

    public static ProbeProgressResponse FromEntity(ProbeProgress entity)
    {
        var auditTrail = entity.AuditEvents
            .OrderBy(x => x.OccurredAtUtc)
            .Select(x => new ProbeProgressAuditEventResponse(
                x.ProbeProgressAuditEventKey,
                x.FromStatus,
                x.ToStatus,
                x.Action,
                x.ActorUserKey,
                x.ActorName,
                x.ActorRole,
                x.OccurredAtUtc,
                x.Note))
            .ToList();

        return new ProbeProgressResponse
        {
            ProbeProgressKey = entity.ProbeProgressKey,
            MemberKey = entity.MemberKey,
            KurinKey = entity.KurinKey,
            ProbeId = entity.ProbeId,
            Status = entity.Status,
            CompletedAtUtc = entity.CompletedAtUtc,
            CompletedByUserKey = entity.CompletedByUserKey,
            CompletedByName = entity.CompletedByName,
            CompletedByRole = entity.CompletedByRole,
            VerifiedAtUtc = entity.VerifiedAtUtc,
            VerifiedByUserKey = entity.VerifiedByUserKey,
            VerifiedByName = entity.VerifiedByName,
            VerifiedByRole = entity.VerifiedByRole,
            AuditTrail = auditTrail
        };
    }
}
