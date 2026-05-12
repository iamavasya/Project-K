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
    public class MemberWarningRepository : IMemberWarningRepository
    {
        private readonly AppDbContext _context;

        public MemberWarningRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Create(MemberWarning entity, CancellationToken cancellationToken = default)
        {
            _context.MemberWarnings.Add(entity);
        }

        public void Delete(MemberWarning entity, CancellationToken cancellationToken = default)
        {
            _context.MemberWarnings.Remove(entity);
        }

        public async Task<MemberWarning?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.MemberWarnings
                .AsTracking()
                .FirstOrDefaultAsync(x => x.MemberWarningKey == entityKey, cancellationToken);
        }

        public async Task<IEnumerable<MemberWarning>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.MemberWarnings
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.MemberWarnings
                .AnyAsync(x => x.MemberWarningKey == entityKey, cancellationToken);
        }

        public void Update(MemberWarning entity, CancellationToken cancellationToken = default)
        {
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                _context.MemberWarnings.Update(entity);
                return;
            }

            entry.State = EntityState.Modified;
        }

        public async Task<IReadOnlyCollection<MemberWarning>> GetByMemberKeyAsync(Guid memberKey, CancellationToken cancellationToken = default)
        {
            return await _context.MemberWarnings
                .Where(x => x.MemberKey == memberKey)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyCollection<MemberWarning>> GetActiveByMemberKeyAsync(Guid memberKey, DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            return await _context.MemberWarnings
                .Where(x => x.MemberKey == memberKey && x.RevokedAtUtc == null && x.ExpiresAtUtc > nowUtc)
                .AsTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<MemberWarning?> GetActiveByMemberAndLevelAsync(Guid memberKey, MemberWarningLevel level, DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            return await _context.MemberWarnings
                .Where(x => x.MemberKey == memberKey && x.Level == level && x.RevokedAtUtc == null && x.ExpiresAtUtc > nowUtc)
                .AsTracking()
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<int> ExpireActiveWarningsAsync(DateTime nowUtc, CancellationToken cancellationToken = default)
        {
            return await _context.MemberWarnings
                .Where(w => w.RevokedAtUtc == null && w.ExpiresAtUtc <= nowUtc)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(w => w.RevokedAtUtc, nowUtc)
                    .SetProperty(w => w.UpdatedDate, nowUtc),
                    cancellationToken);
        }
    }
}
