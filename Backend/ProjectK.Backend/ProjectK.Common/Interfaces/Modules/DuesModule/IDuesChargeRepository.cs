using ProjectK.Common.Entities.DuesModule;

namespace ProjectK.Common.Interfaces.Modules.DuesModule;

public interface IDuesChargeRepository : IBaseEntityRepository<DuesCharge>
{
    /// <summary>Every charge in the kurin.</summary>
    Task<IReadOnlyList<DuesCharge>> GetForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);
}
