using ProjectK.Common.Entities.DuesModule;

namespace ProjectK.Common.Interfaces.Modules.DuesModule;

public interface IDuesChargeRepository : IBaseEntityRepository<DuesCharge>
{
    /// <summary>Every charge in the kurin.</summary>
    Task<IReadOnlyList<DuesCharge>> GetForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);

    /// <summary>Drops every charge of these memberships — the people behind them are gone. Through the tracker, so it commits with the caller.</summary>
    Task DeleteForMembershipsAsync(IReadOnlyCollection<Guid> membershipKeys, CancellationToken cancellationToken = default);
}
