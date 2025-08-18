using Microsoft.EntityFrameworkCore;
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
    public class GroupRepository : IGroupRepository
    {
        private readonly AppDbContext _context;
        public GroupRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Create(Group group, CancellationToken token = default)
        {
            _context.Groups.Add(group);
        }

        public void Delete(Group group, CancellationToken token = default)
        {
            _context.Groups.Remove(group);
        }

        public async Task<Group?> GetByKeyAsync(Guid entityKey, CancellationToken token = default)
        {
            return await _context.Groups.Include(g => g.Kurin).FirstOrDefaultAsync(e => e.GroupKey == entityKey, token);
        }

        public async Task<IEnumerable<Group>> GetAllAsync(Guid kurinKey, CancellationToken token = default)
        {
            return await _context.Groups.Where(g => g.KurinKey == kurinKey).Include(g => g.Kurin).AsNoTracking().ToListAsync(token);
        }

        public Task<IEnumerable<Group>> GetAllAsync(CancellationToken token = default)
        {
            throw new NotSupportedException("Use GetAllAsync(Guid kurinKey) instead.");
        }

        public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken token = default)
        {
            return await _context.Groups.AnyAsync(e => e.GroupKey == entityKey, token);
        }

        public void Update(Group group, CancellationToken token = default)
        {
            _context.Groups.Update(group);
        }
    }
}
