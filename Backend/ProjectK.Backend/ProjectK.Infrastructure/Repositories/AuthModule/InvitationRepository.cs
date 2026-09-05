using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Infrastructure.DbContexts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Repositories.AuthModule
{
    public class InvitationRepository : BaseEntityRepository<Invitation>, IInvitationRepository
    {

        public InvitationRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<Invitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
        {
            return await Context.Invitations.FirstOrDefaultAsync(e => e.Token == token && !e.IsRevoked && e.UsedAtUtc == null, cancellationToken);
        }

        public async Task<Invitation?> GetActiveByWaitlistEntryKeyAsync(Guid waitlistEntryKey, CancellationToken cancellationToken = default)
        {
            return await Context.Invitations.FirstOrDefaultAsync(e => e.WaitlistEntryKey == waitlistEntryKey && !e.IsRevoked && e.UsedAtUtc == null, cancellationToken);
        }

    }
}
