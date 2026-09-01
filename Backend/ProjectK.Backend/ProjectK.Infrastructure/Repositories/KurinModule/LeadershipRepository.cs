using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Authorization;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.DbContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectK.Common.Models.Dtos.KurinModule;

namespace ProjectK.Infrastructure.Repositories.KurinModule
{
    public class LeadershipRepository : ILeadershipRepository
    {
        private readonly AppDbContext _context;
        public LeadershipRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Leadership?> GetByKeyAsync(Guid leadershipKey, CancellationToken cancellationToken = default)
        {
            return await _context.Leaderships
                                 .Include(l => l.LeadershipHistories)
                                    .ThenInclude(h => h.Member)
                                 .FirstOrDefaultAsync(l => l.LeadershipKey == leadershipKey, cancellationToken);
        }

        public async Task<IEnumerable<Leadership>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.Leaderships
                                 .Include(l => l.LeadershipHistories)
                                 .AsNoTracking()
                                 .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Leadership>> GetAllByTypeAsync(LeadershipType type, Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.Leaderships
                                 .Where(l =>
                                    l.Type == type &&
                                    (
                                        (type == LeadershipType.Kurin || type == LeadershipType.KV)
                                        && l.KurinKey == entityKey
                                    )
                                    ||
                                    (
                                        type == LeadershipType.Group
                                        && l.GroupKey == entityKey
                                    )
                                 )
                                 .Include(l => l.LeadershipHistories)
                                    .ThenInclude(h => h.Member)
                                 .AsNoTracking()
                                 .ToListAsync(cancellationToken);
        }

        public void Add(Leadership leadership, CancellationToken cancellationToken = default)
        {
            _context.Leaderships.AddAsync(leadership, cancellationToken);
        }

        public void Update(Leadership leadership, CancellationToken cancellationToken = default)
        {
            _context.Leaderships.Update(leadership);
        }

        /// <summary>
        /// Loaded tracked and without its history, unlike <see cref="GetAllByTypeAsync"/>: that one
        /// reads <c>AsNoTracking</c> with the members attached, and re-attaching that graph for a
        /// delete would put a second copy of an already-tracked member in front of EF.
        /// </summary>
        public async Task DeleteForGroupAsync(Guid groupKey, CancellationToken cancellationToken = default)
        {
            var leaderships = await _context.Leaderships
                .Where(leadership => leadership.GroupKey == groupKey)
                .ToListAsync(cancellationToken);

            _context.Leaderships.RemoveRange(leaderships);
        }

        public async Task<IEnumerable<LeadershipHistory>> GetLeadershipHistoriesAsync(Guid leadershipKey, CancellationToken cancellationToken = default)
        {
            return await _context.LeadershipHistories
                                 .Where(h => h.LeadershipKey == leadershipKey)
                                 .Include(h => h.Member)
                                 .AsNoTracking()
                                 .ToListAsync(cancellationToken);
        }

        public void LeadershipHistoriesRemoveRange(IEnumerable<LeadershipHistory> histories)
        {
            _context.LeadershipHistories.RemoveRange(histories);
        }

        public async Task<IReadOnlyList<MemberOffice>> GetActiveOfficesForMemberAsync(Guid memberKey, CancellationToken cancellationToken = default)
        {
            return await _context.LeadershipHistories
                                 .Where(h => h.MemberKey == memberKey && h.EndDate == null)
                                 .Join(_context.Leaderships,
                                       h => h.LeadershipKey,
                                       l => l.LeadershipKey,
                                       (h, l) => new MemberOffice(l.Type, h.Role))
                                 .Distinct()
                                 .AsNoTracking()
                                 .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Guid>> GetActiveOfficeMemberKeysAsync(
            IReadOnlyCollection<LeadershipRole> roles,
            Guid? kurinKey = null,
            Guid? groupKey = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.LeadershipHistories
                                .Where(h => h.EndDate == null && roles.Contains(h.Role))
                                .Join(_context.Leaderships,
                                      h => h.LeadershipKey,
                                      l => l.LeadershipKey,
                                      (h, l) => new { h.MemberKey, l.KurinKey, l.GroupKey });

            if (kurinKey.HasValue)
            {
                query = query.Where(x => x.KurinKey == kurinKey.Value);
            }

            if (groupKey.HasValue)
            {
                query = query.Where(x => x.GroupKey == groupKey.Value);
            }

            return await query
                         .Select(x => x.MemberKey)
                         .Distinct()
                         .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<MemberLookupDto>> GetOfficeMembersLookupAsync(
            Guid kurinKey,
            LeadershipType type,
            CancellationToken cancellationToken = default)
        {
            var rows = await _context.LeadershipHistories
                                     .Where(h => h.EndDate == null)
                                     .Join(_context.Leaderships.Where(l => l.Type == type && l.KurinKey == kurinKey),
                                           h => h.LeadershipKey,
                                           l => l.LeadershipKey,
                                           (h, l) => h)
                                     .Join(_context.Members,
                                           h => h.MemberKey,
                                           m => m.MemberKey,
                                           (h, m) => new { m.MemberKey, m.UserKey, m.FirstName, m.MiddleName, m.LastName, h.Role })
                                     .AsNoTracking()
                                     .ToListAsync(cancellationToken);

            return rows
                .Select(r => new MemberLookupDto
                {
                    MemberKey = r.MemberKey,
                    UserKey = r.UserKey,
                    FirstName = r.FirstName,
                    MiddleName = r.MiddleName,
                    LastName = r.LastName,
                    // The system-role name, not the bare enum: MemberLookupDto.UserRole is read as an
                    // office identity, and MemberRepository fills the same field the same way.
                    UserRole = SystemRole.ForOffice(type, r.Role)
                })
                .ToList();
        }

        public async Task<IReadOnlyList<LeadershipRef>> GetLeadershipRefsForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default)
        {
            var groupKeys = await _context.Groups
                .Where(g => g.KurinKey == kurinKey)
                .Select(g => g.GroupKey)
                .ToListAsync(cancellationToken);

            return await _context.Leaderships
                .Where(l => l.KurinKey == kurinKey || (l.GroupKey != null && groupKeys.Contains(l.GroupKey.Value)))
                .Select(l => new LeadershipRef(l.LeadershipKey, l.Type, l.GroupKey))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Guid>> GetActiveMemberUserKeysForLeadershipAsync(Guid leadershipKey, CancellationToken cancellationToken = default)
        {
            return await _context.LeadershipHistories
                .Where(h => h.LeadershipKey == leadershipKey && h.EndDate == null)
                .Join(_context.Members, h => h.MemberKey, m => m.MemberKey, (h, m) => m.UserKey)
                .Where(userKey => userKey != null)
                .Select(userKey => userKey!.Value)
                .Distinct()
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Guid>> GetActiveLeadershipKeysForMemberAsync(Guid memberKey, CancellationToken cancellationToken = default)
        {
            return await _context.LeadershipHistories
                .Where(h => h.MemberKey == memberKey && h.EndDate == null)
                .Select(h => h.LeadershipKey)
                .Distinct()
                .ToListAsync(cancellationToken);
        }
    }
}
