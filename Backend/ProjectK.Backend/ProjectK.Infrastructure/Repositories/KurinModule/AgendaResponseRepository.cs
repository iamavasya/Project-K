using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories.KurinModule;

public class AgendaResponseRepository : BaseEntityRepository<AgendaResponse>, IAgendaResponseRepository
{

    public AgendaResponseRepository(AppDbContext context) : base(context)
        {
        }

    public override async Task<AgendaResponse?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default) =>
        await Context.AgendaResponses.FirstOrDefaultAsync(r => r.AgendaResponseKey == entityKey, cancellationToken);

    public override async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default) =>
        await Context.AgendaResponses.AnyAsync(r => r.AgendaResponseKey == entityKey, cancellationToken);

    public override Task<IEnumerable<AgendaResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Use GetForItemAsync instead.");

    public async Task<IReadOnlyList<AgendaResponse>> GetForItemAsync(Guid agendaItemKey, CancellationToken cancellationToken = default) =>
        await Context.AgendaResponses
            .Where(r => r.AgendaItemKey == agendaItemKey)
            .OrderBy(r => r.RespondedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<AgendaResponse?> GetForItemAndUserAsync(Guid agendaItemKey, Guid userKey, CancellationToken cancellationToken = default) =>
        await Context.AgendaResponses.FirstOrDefaultAsync(r => r.AgendaItemKey == agendaItemKey && r.UserKey == userKey, cancellationToken);
}
