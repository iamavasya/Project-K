using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.DuesModule;
using ProjectK.Common.Interfaces.Modules.DuesModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories.DuesModule;

public class DuesChargeRepository : BaseEntityRepository<DuesCharge>, IDuesChargeRepository
{
    public DuesChargeRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<DuesCharge>> GetForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default)
        => await Set
            .AsNoTracking()
            .Where(e => e.KurinKey == kurinKey)
            .OrderBy(e => e.Quarter)
            .ToListAsync(cancellationToken);
}
