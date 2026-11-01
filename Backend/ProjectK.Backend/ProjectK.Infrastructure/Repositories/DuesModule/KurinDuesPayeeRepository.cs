using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.DuesModule;
using ProjectK.Common.Interfaces.Modules.DuesModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories.DuesModule;

public class KurinDuesPayeeRepository : BaseEntityRepository<KurinDuesPayee>, IKurinDuesPayeeRepository
{
    public KurinDuesPayeeRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<KurinDuesPayee>> GetForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default)
        => await Set.Where(e => e.KurinKey == kurinKey).OrderBy(e => e.Name).ToListAsync(cancellationToken);
}
