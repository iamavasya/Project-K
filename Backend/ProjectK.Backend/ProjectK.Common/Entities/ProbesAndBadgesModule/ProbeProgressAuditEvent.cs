using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.Entities;

namespace ProjectK.Common.Entities.ProbesAndBadgesModule;

public class ProbeProgressAuditEvent : Entity
{
    public Guid ProbeProgressAuditEventKey { get; set; } = Guid.NewGuid();
    public Guid ProbeProgressKey { get; set; }
    public ProbeProgressStatus? FromStatus { get; set; }
    public ProbeProgressStatus ToStatus { get; set; }
    public string Action { get; set; } = string.Empty;
    public Guid? ActorUserKey { get; set; }
    public string? ActorName { get; set; }
    public string ActorRole { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }

    public ProbeProgress ProbeProgress { get; set; } = null!;
}
