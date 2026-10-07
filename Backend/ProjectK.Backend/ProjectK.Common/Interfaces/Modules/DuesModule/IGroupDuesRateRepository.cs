using ProjectK.Common.Entities.DuesModule;

namespace ProjectK.Common.Interfaces.Modules.DuesModule;

public interface IGroupDuesRateRepository : IBaseEntityRepository<GroupDuesRate>
{
    /// <summary>Every гурток rate in the kurin, oldest first.</summary>
    Task<IReadOnlyList<GroupDuesRate>> GetForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);
}
