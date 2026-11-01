using ProjectK.Common.Entities.DuesModule;

namespace ProjectK.Common.Interfaces.Modules.DuesModule;

public interface IKurinDuesPayeeRepository : IBaseEntityRepository<KurinDuesPayee>
{
    /// <summary>The kurin's payees, archived ones too, by name, tracked.</summary>
    Task<IReadOnlyList<KurinDuesPayee>> GetForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);
}
