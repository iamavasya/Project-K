using ProjectK.Common.Entities.ProbesAndBadgesModule;

namespace ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;

public interface IProbePointProgressRepository : IBaseEntityRepository<ProbePointProgress>
{
    Task<ProbePointProgress?> GetByMemberProbePointAsync(
        Guid memberKey,
        string probeId,
        string pointId,
        CancellationToken cancellationToken = default);

    Task<IEnumerable<ProbePointProgress>> GetByMemberAndProbeAsync(
        Guid memberKey,
        string probeId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes these people's signed probe points. The member row no longer owns these through a foreign key,
    /// so deleting a member — or a whole kurin of them — has to ask for this explicitly.
    /// </summary>
    Task DeleteForMembersAsync(IReadOnlyCollection<Guid> memberKeys, CancellationToken cancellationToken = default);
}
