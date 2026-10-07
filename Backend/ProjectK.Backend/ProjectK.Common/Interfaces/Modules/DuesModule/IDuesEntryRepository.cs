using ProjectK.Common.Entities.DuesModule;

namespace ProjectK.Common.Interfaces.Modules.DuesModule;

public interface IDuesEntryRepository : IBaseEntityRepository<DuesEntry>
{
    /// <summary>Every entry in the kurin that has not been deleted, untracked.</summary>
    Task<IReadOnlyList<DuesEntry>> GetForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);
}
