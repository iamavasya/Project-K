using ProjectK.Common.Entities.ProbesAndBadgesModule;

namespace ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;

public interface IProbeProgressRepository : IBaseEntityRepository<ProbeProgress>
{
    Task<ProbeProgress?> GetByMemberAndProbeIdAsync(Guid memberKey, string probeId, CancellationToken cancellationToken = default);
    Task<ProbeProgress?> GetByMemberAndProbeIdWithAuditAsync(Guid memberKey, string probeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ProbeProgress>> GetByMemberKeyAsync(Guid memberKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes these people's probe progress. The member row no longer owns these through a foreign key,
    /// so deleting a member — or a whole kurin of them — has to ask for this explicitly.
    /// </summary>
    Task DeleteForMembersAsync(IReadOnlyCollection<Guid> memberKeys, CancellationToken cancellationToken = default);
}
