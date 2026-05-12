using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.DbContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Repositories
{
    public class MemberAwardRepository : IMemberAwardRepository
    {
        private readonly AppDbContext _context;

        public MemberAwardRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Create(MemberAward entity, CancellationToken cancellationToken = default)
        {
            _context.MemberAwards.Add(entity);
        }

        public void Delete(MemberAward entity, CancellationToken cancellationToken = default)
        {
            _context.MemberAwards.Remove(entity);
        }

        public async Task<MemberAward?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.MemberAwards
                .AsTracking()
                .FirstOrDefaultAsync(x => x.MemberAwardKey == entityKey, cancellationToken);
        }

        public async Task<IEnumerable<MemberAward>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.MemberAwards
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.MemberAwards
                .AnyAsync(x => x.MemberAwardKey == entityKey, cancellationToken);
        }

        public void Update(MemberAward entity, CancellationToken cancellationToken = default)
        {
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                _context.MemberAwards.Update(entity);
                return;
            }

            entry.State = EntityState.Modified;
        }

        public async Task<IReadOnlyCollection<MemberAward>> GetByMemberKeyAsync(Guid memberKey, CancellationToken cancellationToken = default)
        {
            return await _context.MemberAwards
                .Where(x => x.MemberKey == memberKey)
                .OrderBy(x => x.Level)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<MemberAward?> GetByMemberAndLevelAsync(Guid memberKey, MemberAwardLevel level, CancellationToken cancellationToken = default)
        {
            return await _context.MemberAwards
                .Where(x => x.MemberKey == memberKey && x.Level == level)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken);
        }
    }
}
