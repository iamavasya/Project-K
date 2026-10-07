using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.ScoreModule;
using ProjectK.Common.Interfaces.Modules.ScoreModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories.ScoreModule;

public class ScoreTrailEventRepository : BaseEntityRepository<ScoreTrailEvent>, IScoreTrailEventRepository
{
    public ScoreTrailEventRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ScoreTrailEvent>> GetForSubjectAsync(Guid subjectKey, CancellationToken cancellationToken = default)
        => await Set
            .AsNoTracking()
            .Where(e => e.SubjectKey == subjectKey)
            .OrderBy(e => e.OccurredAtUtc)
            .ToListAsync(cancellationToken);

    /// <summary>The trail is append-only.</summary>
    public override void Delete(ScoreTrailEvent entity, CancellationToken cancellationToken = default)
        => throw new InvalidOperationException("The score trail is never deleted from.");
}
