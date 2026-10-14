using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KraiModule;
using ProjectK.Common.Interfaces.Modules.KraiModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories.KraiModule;

public class KraiRepository : IKraiRepository
{
    private readonly AppDbContext _context;

    public KraiRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Krai>> GetAllAsync(CancellationToken token = default)
    {
        return await _context.Krais.AsNoTracking().OrderBy(k => k.Code).ToListAsync(token);
    }

    public Task<bool> ExistsAsync(string code, CancellationToken token = default)
    {
        return _context.Krais.AnyAsync(k => k.Code == code, token);
    }

    public Task<Krai?> GetAsync(string code, CancellationToken token = default)
    {
        return _context.Krais.AsNoTracking().FirstOrDefaultAsync(k => k.Code == code, token);
    }

    public Task<KraiProfileRevision?> GetLatestProfileRevisionAsync(string code, CancellationToken token = default)
    {
        return _context.KraiProfileRevisions
            .AsNoTracking()
            .Where(r => r.KraiCode == code)
            .OrderByDescending(r => r.Revision)
            .FirstOrDefaultAsync(token);
    }
}
