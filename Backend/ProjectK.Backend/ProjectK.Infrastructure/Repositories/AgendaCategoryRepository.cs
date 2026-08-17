using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories;

public class AgendaCategoryRepository : IAgendaCategoryRepository
{
    private readonly AppDbContext _context;

    public AgendaCategoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public void Create(AgendaCategory entity, CancellationToken cancellationToken = default) => _context.AgendaCategories.Add(entity);

    public void Update(AgendaCategory entity, CancellationToken cancellationToken = default) => _context.AgendaCategories.Update(entity);

    public void Delete(AgendaCategory entity, CancellationToken cancellationToken = default) => _context.AgendaCategories.Remove(entity);

    public async Task<AgendaCategory?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default) =>
        await _context.AgendaCategories.FirstOrDefaultAsync(c => c.AgendaCategoryKey == entityKey, cancellationToken);

    public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default) =>
        await _context.AgendaCategories.AnyAsync(c => c.AgendaCategoryKey == entityKey, cancellationToken);

    public Task<IEnumerable<AgendaCategory>> GetAllAsync(CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Use GetForKurinAsync instead.");

    public async Task<IReadOnlyList<AgendaCategory>> GetForKurinAsync(Guid kurinKey, bool includeArchived, CancellationToken cancellationToken = default)
    {
        var query = _context.AgendaCategories.Where(c => c.KurinKey == kurinKey);
        if (!includeArchived)
        {
            query = query.Where(c => !c.IsArchived);
        }

        return await query
            .OrderBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
