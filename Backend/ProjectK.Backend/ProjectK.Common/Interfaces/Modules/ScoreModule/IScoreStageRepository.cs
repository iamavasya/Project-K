using ProjectK.Common.Entities.ScoreModule;

namespace ProjectK.Common.Interfaces.Modules.ScoreModule;

public interface IScoreStageRepository : IBaseEntityRepository<ScoreStage>
{
    /// <summary>The kurin's stages, earliest first, untracked.</summary>
    Task<IReadOnlyList<ScoreStage>> GetForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);
}
