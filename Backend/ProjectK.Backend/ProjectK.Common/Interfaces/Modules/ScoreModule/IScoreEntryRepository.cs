using ProjectK.Common.Entities.ScoreModule;

namespace ProjectK.Common.Interfaces.Modules.ScoreModule;

public interface IScoreEntryRepository : IBaseEntityRepository<ScoreEntry>
{
    /// <summary>Every entry in the kurin that has not been deleted, untracked.</summary>
    Task<IReadOnlyList<ScoreEntry>> GetForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);
}
