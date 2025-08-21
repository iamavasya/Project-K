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
    public class MemberRepository : IMemberRepository
    {
        private readonly AppDbContext _context;
        public MemberRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Create(Member member, CancellationToken token = default)
        {
            _context.Members.Add(member);
        }

        public void Delete(Member member, CancellationToken token = default)
        {
            _context.Members.Remove(member);
        }

        public async Task<Member?> GetByKeyAsync(Guid entityKey, CancellationToken token = default)
        {
            return await _context.Members.Include(m => m.Group)
                                         .Include(m => m.Kurin)
                                         .FirstOrDefaultAsync(e => e.MemberKey == entityKey, token);
        }

        public async Task<IEnumerable<Member>> GetAllAsync(Guid groupKey, CancellationToken token = default)
        {
            return await _context.Members.Where(m => m.GroupKey == groupKey)
                                         .Include(m => m.Group)
                                         .Include(m => m.Kurin)
                                         .AsNoTracking()
                                         .ToListAsync(token);
        }

        public async Task<IEnumerable<Member>> GetAllByKurinKeyAsync(Guid kurinKey, CancellationToken token = default)
        {
            return await _context.Members.Where(m => m.KurinKey == kurinKey)
                                         .Include(m => m.Group)
                                         .Include(m => m.Kurin)
                                         .AsNoTracking()
                                         .ToListAsync(token);
        }

        public Task<IEnumerable<Member>> GetAllAsync(CancellationToken token = default)
        {
            throw new NotSupportedException("Use GetAllAsync(Guid groupKey, CancellationToken token) or GetAllByKurinkey(...) instead.");
        }

        public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken token = default)
        {
            return await _context.Members.AnyAsync(e => e.MemberKey == entityKey, token);
        }

        public void Update(Member member, CancellationToken token = default)
        {
            _context.Members.Update(member);
        }
    }
}
