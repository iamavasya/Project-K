using ProjectK.Common.Entities.ScoreModule;

namespace ProjectK.Common.Interfaces.Modules.ScoreModule;

public interface IScoreRuleRepository : IBaseEntityRepository<ScoreRule>
{
    /// <summary>Every rule the kurin has had, oldest first within a source, untracked.</summary>
    Task<IReadOnlyList<ScoreRule>> GetForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);
}
