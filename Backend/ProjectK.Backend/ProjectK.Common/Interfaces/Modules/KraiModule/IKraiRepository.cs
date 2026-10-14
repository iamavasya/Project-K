using ProjectK.Common.Entities.KraiModule;

namespace ProjectK.Common.Interfaces.Modules.KraiModule;

public interface IKraiRepository
{
    Task<IReadOnlyList<Krai>> GetAllAsync(CancellationToken token = default);

    Task<bool> ExistsAsync(string code, CancellationToken token = default);
}
