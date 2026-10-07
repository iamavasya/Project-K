using ProjectK.Common.Entities.DuesModule;

namespace ProjectK.Common.Interfaces.Modules.DuesModule;

public interface IDuesConcessionRepository : IBaseEntityRepository<DuesConcession>
{
    /// <summary>Every пільга switch in the kurin, oldest first.</summary>
    Task<IReadOnlyList<DuesConcession>> GetForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);
}
