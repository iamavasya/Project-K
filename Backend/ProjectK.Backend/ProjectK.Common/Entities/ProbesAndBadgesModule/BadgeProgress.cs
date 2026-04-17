using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.Entities;

namespace ProjectK.Common.Entities.ProbesAndBadgesModule;

public class BadgeProgress : Entity
{
    public Guid BadgeProgressKey { get; set; } = Guid.NewGuid();
    public Guid MemberKey { get; set; }
    public Guid KurinKey { get; set; }
    public string BadgeId { get; set; } = string.Empty;
    public BadgeProgressStatus Status { get; set; } = BadgeProgressStatus.Draft;
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public Guid? ReviewedByUserKey { get; set; }
    public string? ReviewedByName { get; set; }
    public string? ReviewedByRole { get; set; }
    public string? ReviewNote { get; set; }

    public Member Member { get; set; } = null!;
    public ICollection<BadgeProgressAuditEvent> AuditEvents { get; set; } = new List<BadgeProgressAuditEvent>();
}
