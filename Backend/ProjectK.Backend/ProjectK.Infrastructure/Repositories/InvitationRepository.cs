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
    public class InvitationRepository : IInvitationRepository
    {
        private readonly AppDbContext _context;

        public InvitationRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Create(Invitation entity, CancellationToken cancellationToken = default)
        {
            _context.Invitations.Add(entity);
        }

        public async Task<Invitation?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.Invitations.FirstOrDefaultAsync(e => e.InvitationKey == entityKey, cancellationToken);
        }

        public async Task<Invitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            return await _context.Invitations.FirstOrDefaultAsync(e => e.Token == token && !e.IsRevoked && e.UsedAtUtc == null, cancellationToken);
        }

        public async Task<Invitation?> GetActiveByWaitlistEntryKeyAsync(Guid waitlistEntryKey, CancellationToken cancellationToken = default)
        {
            return await _context.Invitations.FirstOrDefaultAsync(e => e.WaitlistEntryKey == waitlistEntryKey && !e.IsRevoked && e.UsedAtUtc == null, cancellationToken);
        }

        public async Task<IEnumerable<Invitation>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Invitations.ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.Invitations.AnyAsync(e => e.InvitationKey == entityKey, cancellationToken);
        }

        public void Update(Invitation entity, CancellationToken cancellationToken = default)
        {
            _context.Invitations.Update(entity);
        }

        public void Delete(Invitation entity, CancellationToken cancellationToken = default)
        {
            _context.Invitations.Remove(entity);
        }
    }
}
