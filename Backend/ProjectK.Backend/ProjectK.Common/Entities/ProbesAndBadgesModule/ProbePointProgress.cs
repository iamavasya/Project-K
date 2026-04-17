using ProjectK.Common.Entities.KurinModule;
using ProjectK.Infrastructure.Entities;

namespace ProjectK.Common.Entities.ProbesAndBadgesModule;

public class ProbePointProgress : Entity
{
    public Guid ProbePointProgressKey { get; set; } = Guid.NewGuid();
    public Guid MemberKey { get; set; }
    public Guid KurinKey { get; set; }
    public string ProbeId { get; set; } = string.Empty;
    public string PointId { get; set; } = string.Empty;
    public bool IsSigned { get; set; }
    public DateTime? SignedAtUtc { get; set; }
    public Guid? SignedByUserKey { get; set; }
    public string? SignedByName { get; set; }
    public string? SignedByRole { get; set; }

    public Member Member { get; set; } = null!;
}
