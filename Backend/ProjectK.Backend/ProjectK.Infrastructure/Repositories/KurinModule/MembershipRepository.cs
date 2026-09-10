using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
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

        public async Task<IReadOnlyCollection<MembershipRecord>> GetRecordsForMemberAsync(
            Guid memberKey,
            CancellationToken cancellationToken = default)
            => await AsRecords(Context.Memberships.Where(m => m.MemberKey == memberKey))
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyCollection<MembershipRecord>> GetCurrentRecordsForAccountAsync(
            Guid userKey,
            CancellationToken cancellationToken = default)
            => await AsRecords(Context.Memberships.Where(m => m.UserKey == userKey && m.LeftAtUtc == null))
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<MembershipRecord>>> GetCurrentRecordsForAccountsAsync(
            IReadOnlyCollection<Guid> userKeys,
            CancellationToken cancellationToken = default)
        {
            if (userKeys.Count == 0)
            {
                return new Dictionary<Guid, IReadOnlyCollection<MembershipRecord>>();
            }

            var keys = userKeys.Distinct().ToList();
            var rows = await Context.Memberships
                .Where(m => m.UserKey != null && keys.Contains(m.UserKey.Value) && m.LeftAtUtc == null)
                .OrderByDescending(m => m.JoinedAtUtc)
                .Select(m => new
                {
                    UserKey = m.UserKey!.Value,
                    Record = new MembershipRecord(
                        m.MembershipKey,
                        m.KurinKey,
                        m.Kurin.Number,
                        m.Kurin.Branch,
                        m.Kurin.NamedAfter,
                        m.GroupKey,
                        m.Group != null ? m.Group.Name : null,
                        m.Kind,
                        m.JoinedAtUtc,
                        m.LeftAtUtc)
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return rows
                .GroupBy(row => row.UserKey)
                .ToDictionary(
                    group => group.Key,
                    group => (IReadOnlyCollection<MembershipRecord>)group.Select(row => row.Record).ToList());
        }

        public async Task<IReadOnlyCollection<Guid>> GetAccountKeysInKurinAsync(
            Guid kurinKey,
            CancellationToken cancellationToken = default)
            => await Context.Memberships
                .Where(m => m.KurinKey == kurinKey && m.LeftAtUtc == null && m.UserKey != null)
                .Select(m => m.UserKey!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);

        /// <summary>Current memberships first, then the ones already closed, newest joined first.</summary>
        private static IQueryable<MembershipRecord> AsRecords(IQueryable<Membership> memberships)
            => memberships
                .AsNoTracking()
                .OrderBy(m => m.LeftAtUtc == null ? 0 : 1)
                .ThenByDescending(m => m.JoinedAtUtc)
                .Select(m => new MembershipRecord(
                    m.MembershipKey,
                    m.KurinKey,
                    m.Kurin.Number,
                    m.Kurin.Branch,
                    m.Kurin.NamedAfter,
                    m.GroupKey,
                    m.Group != null ? m.Group.Name : null,
                    m.Kind,
                    m.JoinedAtUtc,
                    m.LeftAtUtc));

        public Task<int> CountForMemberAsync(Guid memberKey, CancellationToken cancellationToken = default)
            => Context.Memberships.CountAsync(m => m.MemberKey == memberKey, cancellationToken);

        public async Task<IReadOnlyCollection<Guid>> GetKurinKeysForAccountAsync(
            Guid userKey,
            CancellationToken cancellationToken = default)
            => await Context.Memberships
                .Where(m => m.UserKey == userKey && m.LeftAtUtc == null)
                .Select(m => m.KurinKey)
                .Distinct()
                .ToListAsync(cancellationToken);

        public async Task SyncAccountAsync(
            Guid memberKey,
            Guid? userKey,
            CancellationToken cancellationToken = default)
        {
            foreach (var membership in await CurrentOfAsync(memberKey, cancellationToken))
            {
                membership.UserKey = userKey;
            }
        }

        public Task<Membership?> GetActiveAsync(
            Guid memberKey,
            Guid kurinKey,
            CancellationToken cancellationToken = default)
            => Context.Memberships.FirstOrDefaultAsync(
                m => m.MemberKey == memberKey && m.KurinKey == kurinKey && m.LeftAtUtc == null,
                cancellationToken);

        public void Open(Membership membership) => Context.Memberships.Add(membership);

        public async Task PlaceAsync(
            Guid memberKey,
            Guid? userKey,
            Guid kurinKey,
            Guid? groupKey,
            CancellationToken cancellationToken = default)
        {
            var current = await CurrentOfAsync(memberKey, cancellationToken);
            var here = current.FirstOrDefault(m => m.KurinKey == kurinKey);

            foreach (var elsewhere in current.Where(m => m != here))
            {
                elsewhere.LeftAtUtc = DateTime.UtcNow;
            }

            if (here is null)
            {
                Context.Memberships.Add(new Membership
                {
                    MemberKey = memberKey,
                    UserKey = userKey,
                    KurinKey = kurinKey,
                    GroupKey = groupKey,
                    Kind = MembershipKind.Youth,
                    JoinedAtUtc = DateTime.UtcNow
                });
                return;
            }

            here.GroupKey = groupKey;
            here.UserKey = userKey;
        }

        public async Task RemoveForMembersAsync(
            IReadOnlyCollection<Guid> memberKeys,
            CancellationToken cancellationToken = default)
        {
            if (memberKeys.Count == 0)
            {
                return;
            }

            Context.Memberships.RemoveRange(
                await Context.Memberships
                    .Where(m => memberKeys.Contains(m.MemberKey))
                    .ToListAsync(cancellationToken));
        }

        public async Task RemoveForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default)
            => Context.Memberships.RemoveRange(
                await Context.Memberships
                    .Where(m => m.KurinKey == kurinKey)
                    .ToListAsync(cancellationToken));

        public async Task DetachFromGroupAsync(Guid groupKey, CancellationToken cancellationToken = default)
        {
            var inGroup = await Context.Memberships
                .Where(m => m.GroupKey == groupKey)
                .ToListAsync(cancellationToken);

            foreach (var membership in inGroup)
            {
                membership.GroupKey = null;
            }
        }

        /// <summary>
        /// A person's open memberships as tracked entities, including any opened earlier in this same
        /// unit of work — a member created and placed in one request has no row in the database yet.
        /// </summary>
        private async Task<List<Membership>> CurrentOfAsync(Guid memberKey, CancellationToken cancellationToken)
        {
            var stored = await Context.Memberships
                .Where(m => m.MemberKey == memberKey && m.LeftAtUtc == null)
                .ToListAsync(cancellationToken);

            var pending = Context.Memberships.Local
                .Where(m => m.MemberKey == memberKey && m.LeftAtUtc == null)
                .Where(m => !stored.Contains(m));

            return [.. stored, .. pending];
        }
    }
}
