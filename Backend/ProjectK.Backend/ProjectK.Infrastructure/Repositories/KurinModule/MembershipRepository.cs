using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories.KurinModule
{
    public class MembershipRepository : BaseEntityRepository<Membership>, IMembershipRepository
    {
        public MembershipRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyCollection<Membership>> GetActiveForMemberAsync(
            Guid memberKey,
            CancellationToken cancellationToken = default)
            => await Context.Memberships
                .AsNoTracking()
                .Where(m => m.MemberKey == memberKey && m.LeftAtUtc == null)
                .OrderByDescending(m => m.JoinedAtUtc)
                .ToListAsync(cancellationToken);

        public async Task SyncAccountAsync(
            Guid memberKey,
            Guid? userKey,
            CancellationToken cancellationToken = default)
        {
            var memberships = await Context.Memberships
                .Where(m => m.MemberKey == memberKey && m.LeftAtUtc == null)
                .ToListAsync(cancellationToken);

            foreach (var membership in memberships)
            {
                membership.UserKey = userKey;
            }
        }
    }
}
