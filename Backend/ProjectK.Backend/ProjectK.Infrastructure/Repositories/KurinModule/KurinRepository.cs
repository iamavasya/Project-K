using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Infrastructure.DbContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Repositories.KurinModule
{
    public class KurinRepository : BaseEntityRepository<Kurin>, IKurinRepository
    {
        public KurinRepository(AppDbContext context) : base(context)
        {
        }

        public override void Create(Kurin kurin, CancellationToken token = default)
        {
            Context.Kurins.Add(kurin);
        }

        public override void Delete(Kurin kurin, CancellationToken token = default)
        {
            Context.Kurins.Remove(kurin);
        }

        public override async Task<Kurin?> GetByKeyAsync(Guid entityKey, CancellationToken token = default)
        {
            return await Context.Kurins.Include(k => k.Members).FirstOrDefaultAsync(k => k.KurinKey == entityKey, token);
        }

        public async Task<Kurin?> GetByNumberAsync(int number, CancellationToken token = default)
        {
            return await Context.Kurins.Include(k => k.Members).FirstOrDefaultAsync(k => k.Number == number, token);
        }

        public override async Task<IEnumerable<Kurin>> GetAllAsync(CancellationToken token = default)
        {
            return await Context.Kurins.Include(k => k.Members).ToListAsync(token);
        }

        public async Task<bool> ExistsAsync(int number, CancellationToken token = default)
        {
            return await Context.Kurins.AnyAsync(k => k.Number == number, token);
        }

        public override void Update(Kurin kurin, CancellationToken token = default)
        {
            Context.Kurins.Update(kurin);
        }
    }
}
