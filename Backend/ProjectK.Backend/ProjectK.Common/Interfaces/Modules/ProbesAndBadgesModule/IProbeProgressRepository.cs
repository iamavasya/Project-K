using ProjectK.Common.Entities.ProbesAndBadgesModule;

namespace ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;

public interface IProbeProgressRepository : IBaseEntityRepository<ProbeProgress>
{
    Task<ProbeProgress?> GetByMemberAndProbeIdAsync(Guid memberKey, string probeId, CancellationToken cancellationToken = default);
    Task<ProbeProgress?> GetByMemberAndProbeIdWithAuditAsync(Guid memberKey, string probeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProbeProgress>> GetByMemberKeyAsync(Guid memberKey, CancellationToken cancellationToken = default);
}
