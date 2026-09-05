using ProjectK.Common.Entities.ProbesAndBadgesModule;

namespace ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;

public interface IBadgeProgressRepository : IBaseEntityRepository<BadgeProgress>
{
    Task<BadgeProgress?> GetByMemberAndBadgeIdAsync(Guid memberKey, string badgeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<BadgeProgress>> GetByMemberKeyAsync(Guid memberKey, CancellationToken cancellationToken = default);
    Task<IEnumerable<BadgeProgress>> GetByMemberKeysAsync(IEnumerable<Guid> memberKeys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes these people's badge progress. The member row no longer owns these through a foreign key,
    /// so deleting a member — or a whole kurin of them — has to ask for this explicitly.
    /// </summary>
    Task DeleteForMembersAsync(IReadOnlyCollection<Guid> memberKeys, CancellationToken cancellationToken = default);
}
