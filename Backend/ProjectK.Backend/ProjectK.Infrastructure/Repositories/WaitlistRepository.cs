using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Infrastructure.DbContexts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Repositories
{
    public class WaitlistRepository : IWaitlistRepository
    {
        private readonly AppDbContext _context;

        public WaitlistRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Create(WaitlistEntry entity, CancellationToken cancellationToken = default)
        {
            _context.WaitlistEntries.Add(entity);
        }

        public async Task<WaitlistEntry?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.WaitlistEntries.FirstOrDefaultAsync(e => e.WaitlistEntryKey == entityKey, cancellationToken);
        }

        public async Task<WaitlistEntry?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await _context.WaitlistEntries.FirstOrDefaultAsync(e => e.Email == email, cancellationToken);
        }

        public async Task<IEnumerable<WaitlistEntry>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.WaitlistEntries.ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.WaitlistEntries.AnyAsync(e => e.WaitlistEntryKey == entityKey, cancellationToken);
        }

        public void Update(WaitlistEntry entity, CancellationToken cancellationToken = default)
        {
            _context.WaitlistEntries.Update(entity);
        }

        public void Delete(WaitlistEntry entity, CancellationToken cancellationToken = default)
        {
            _context.WaitlistEntries.Remove(entity);
        }
    }
}
