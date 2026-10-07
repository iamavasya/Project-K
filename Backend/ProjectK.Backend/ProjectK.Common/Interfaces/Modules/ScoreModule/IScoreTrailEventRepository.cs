using ProjectK.Common.Entities.ScoreModule;

namespace ProjectK.Common.Interfaces.Modules.ScoreModule;

public interface IScoreTrailEventRepository : IBaseEntityRepository<ScoreTrailEvent>
{
    /// <summary>What happened to one row, earliest first, untracked.</summary>
    Task<IReadOnlyList<ScoreTrailEvent>> GetForSubjectAsync(Guid subjectKey, CancellationToken cancellationToken = default);
}
