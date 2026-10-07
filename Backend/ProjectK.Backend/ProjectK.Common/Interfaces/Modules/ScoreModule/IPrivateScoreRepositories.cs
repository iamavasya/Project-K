using ProjectK.Common.Entities.ScoreModule;

namespace ProjectK.Common.Interfaces.Modules.ScoreModule;

public interface IPrivateScoreCriterionRepository : IBaseEntityRepository<PrivateScoreCriterion>
{
    /// <summary>The КВ's list, archived ones too, untracked.</summary>
    Task<IReadOnlyList<PrivateScoreCriterion>> GetForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);
}

public interface IPrivateScoreEntryRepository : IBaseEntityRepository<PrivateScoreEntry>
{
    /// <summary>Every entry in the kurin that has not been deleted, untracked.</summary>
    Task<IReadOnlyList<PrivateScoreEntry>> GetForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);
}
