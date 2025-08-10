using Microsoft.EntityFrameworkCore;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Infrastructure.DbContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Repositories
{
    public class KurinRepository : IKurinRepository
    {
        private readonly AppDbContext _context;
        public KurinRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Create(Kurin kurin, CancellationToken token = default)
        {
            _context.Kurins.Add(kurin);
        }

        public void Delete(Kurin kurin, CancellationToken token = default)
        {
            _context.Kurins.Remove(kurin);
        }

        public async Task<Kurin?> GetByKeyAsync(Guid entityKey, CancellationToken token = default)
        {
            return await _context.Kurins.FirstOrDefaultAsync(k => k.KurinKey == entityKey, token);
        }

        public async Task<Kurin?> GetByNumberAsync(int number, CancellationToken token = default)
        {
            return await _context.Kurins.FirstOrDefaultAsync(k => k.Number == number, token);
        }

        public async Task<IEnumerable<Kurin>> GetAllAsync(CancellationToken token = default)
        {
            return await _context.Kurins.ToListAsync(token);
        }

        public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken token = default)
        {
            return await _context.Kurins.AnyAsync(k => k.KurinKey == entityKey, token);
        }

        public async Task<bool> ExistsAsync(int number, CancellationToken token = default)
        {
            return await _context.Kurins.AnyAsync(k => k.Number == number, token);
        }

        public void Update(Kurin kurin, CancellationToken token = default)
        {
            _context.Kurins.Update(kurin);
        }
    }
}
