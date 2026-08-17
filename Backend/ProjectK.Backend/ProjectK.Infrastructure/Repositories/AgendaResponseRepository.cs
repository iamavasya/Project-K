using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories;

public class AgendaResponseRepository : IAgendaResponseRepository
{
    private readonly AppDbContext _context;

    public AgendaResponseRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Create(AgendaResponse entity, CancellationToken cancellationToken = default) => _context.AgendaResponses.Add(entity);

    public void Update(AgendaResponse entity, CancellationToken cancellationToken = default) => _context.AgendaResponses.Update(entity);

    public void Delete(AgendaResponse entity, CancellationToken cancellationToken = default) => _context.AgendaResponses.Remove(entity);

    public async Task<AgendaResponse?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default) =>
        await _context.AgendaResponses.FirstOrDefaultAsync(r => r.AgendaResponseKey == entityKey, cancellationToken);

    public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default) =>
        await _context.AgendaResponses.AnyAsync(r => r.AgendaResponseKey == entityKey, cancellationToken);

    public Task<IEnumerable<AgendaResponse>> GetAllAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Use GetForItemAsync instead.");

    public async Task<IReadOnlyList<AgendaResponse>> GetForItemAsync(Guid agendaItemKey, CancellationToken cancellationToken = default) =>
        await _context.AgendaResponses
            .Where(r => r.AgendaItemKey == agendaItemKey)
            .OrderBy(r => r.RespondedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<AgendaResponse?> GetForItemAndUserAsync(Guid agendaItemKey, Guid userKey, CancellationToken cancellationToken = default) =>
        await _context.AgendaResponses.FirstOrDefaultAsync(r => r.AgendaItemKey == agendaItemKey && r.UserKey == userKey, cancellationToken);
}
