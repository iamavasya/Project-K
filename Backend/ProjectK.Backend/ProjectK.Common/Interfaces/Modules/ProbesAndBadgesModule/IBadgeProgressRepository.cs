using ProjectK.Common.Entities.ProbesAndBadgesModule;

namespace ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;

public interface IBadgeProgressRepository : IBaseEntityRepository<BadgeProgress>
{
    Task<BadgeProgress?> GetByMemberAndBadgeIdAsync(Guid memberKey, string badgeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<BadgeProgress>> GetByMemberKeyAsync(Guid memberKey, CancellationToken cancellationToken = default);
}
