using ProjectK.Common.Entities.KraiModule;

namespace ProjectK.Common.Interfaces.Modules.KraiModule;

public interface IKraiRepository
{
    Task<IReadOnlyList<Krai>> GetAllAsync(CancellationToken token = default);

    Task<bool> ExistsAsync(string code, CancellationToken token = default);

    Task<Krai?> GetAsync(string code, CancellationToken token = default);

    /// <summary>The profile revision in force: the latest one saved for the край.</summary>
    Task<KraiProfileRevision?> GetLatestProfileRevisionAsync(string code, CancellationToken token = default);
}
