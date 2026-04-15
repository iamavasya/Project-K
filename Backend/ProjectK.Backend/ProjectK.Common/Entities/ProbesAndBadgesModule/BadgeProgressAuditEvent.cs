using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.Entities;

namespace ProjectK.Common.Entities.ProbesAndBadgesModule;

public class BadgeProgressAuditEvent : Entity
{
    public Guid BadgeProgressAuditEventKey { get; set; } = Guid.NewGuid();
    public Guid BadgeProgressKey { get; set; }
    public BadgeProgressStatus? FromStatus { get; set; }
    public BadgeProgressStatus ToStatus { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? ActorUserKey { get; set; }
    public string? ActorName { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }

    public BadgeProgress BadgeProgress { get; set; } = null!;
}
