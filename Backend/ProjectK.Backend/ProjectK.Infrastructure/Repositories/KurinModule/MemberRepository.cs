using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
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
using static System.Net.Mime.MediaTypeNames;
using ProjectK.Common.Models.Dtos.KurinModule;
using System.Linq.Expressions;
using ProjectK.Common.Models.Records;

namespace ProjectK.Infrastructure.Repositories.KurinModule
{
    public class MemberRepository : BaseEntityRepository<Member>, IMemberRepository
    {
        private readonly string memberMessage = "Member not found.";
        public MemberRepository(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// Every membership that is still open. Where a person stands in a kurin is said here and
        /// nowhere else, so every read that means "the people of this kurin" or "of this гурток"
        /// starts from this and not from the columns still left on the member record.
        /// </summary>
        private IQueryable<Membership> ActiveMemberships => Context.Memberships.Where(ms => ms.LeftAtUtc == null);

        /// <summary>
        /// The people behind a set of memberships, as a query still rooted in <c>Members</c> — a join
        /// would project the entity out of its own query and take <c>Include</c> with it.
        /// </summary>
        private IQueryable<Member> PeopleOf(IQueryable<Membership> memberships)
            => Context.Members.Where(m => memberships.Any(ms => ms.MemberKey == m.MemberKey));

        public override void Create(Member member, CancellationToken cancellationToken = default)
        {
            Context.Members.Add(member);
        }

        public override void Delete(Member member, CancellationToken cancellationToken = default)
        {
            Context.Members.Remove(member);
        }

        public override async Task<Member?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await Context.Members.Include(m => m.Group)
                                         .Include(m => m.Kurin)
                                         .Include(m => m.PlastLevelHistory)
                                         .Include(m => m.LeadershipHistories)
                                            .ThenInclude(h => h.Leadership)
                                                .ThenInclude(l => l.Group)
                                         .Include(m => m.MemberWarnings)
                                         .Include(m => m.MemberAwards)
                                         .FirstOrDefaultAsync(e => e.MemberKey == entityKey, cancellationToken);
        }

        /// <summary>
        /// The members of one гурток as tracked entities.
        /// <para>
        /// Deliberately bare. It used to eager-load the whole graph <c>AsNoTracking</c>, which made
        /// every member carry its own detached <see cref="Group"/>; removing such a member attached
        /// that copy beside the already-tracked гурток and EF refused the second instance with the
        /// same key, so deleting a гурток answered 500. Nothing is lost: the dependants cascade in
        /// the database, and screens that need the graph read it through
        /// <see cref="GetListItemsByGroupKeyAsync"/>.
        /// </para>
        /// </summary>
        public async Task<IEnumerable<Member>> GetAllAsync(Guid groupKey, CancellationToken cancellationToken = default)
        {
            return await PeopleOf(ActiveMemberships.Where(ms => ms.GroupKey == groupKey))
                                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Member>> GetTrackedForKurinDeletionAsync(Guid kurinKey, CancellationToken cancellationToken = default)
        {
            return await PeopleOf(ActiveMemberships.Where(ms =>
                                    ms.KurinKey == kurinKey
                                    || (ms.GroupKey != null && ms.Group!.KurinKey == kurinKey)))
                                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Member>> GetAllByKurinKeyAsync(Guid kurinKey, CancellationToken cancellationToken = default)
        {
            return await PeopleOf(ActiveMemberships.Where(ms => ms.KurinKey == kurinKey))
                                         .Include(m => m.Group)
                                         .Include(m => m.Kurin)
                                         .Include(m => m.PlastLevelHistory)
                                         .Include(m => m.LeadershipHistories)
                                            .ThenInclude(h => h.Leadership)
                                                .ThenInclude(l => l.Group)
                                         .Include(m => m.MemberWarnings)
                                         .Include(m => m.MemberAwards)
                                         .AsSplitQuery()
                                         .AsNoTracking()
                                         .ToListAsync(cancellationToken);
        }

        public Task<IEnumerable<MemberListItemDto>> GetListItemsByKurinKeyAsync(Guid kurinKey, MemberFieldVisibility visibility, CancellationToken cancellationToken = default)
            => ProjectListItemsAsync(ActiveMemberships.Where(ms => ms.KurinKey == kurinKey), visibility, cancellationToken);

        public Task<IEnumerable<MemberListItemDto>> GetListItemsByGroupKeyAsync(Guid groupKey, MemberFieldVisibility visibility, CancellationToken cancellationToken = default)
            => ProjectListItemsAsync(ActiveMemberships.Where(ms => ms.GroupKey == groupKey), visibility, cancellationToken);

        // Single projection shared by the kurin- and group-scoped list reads. It reads a list of
        // memberships and pulls the person behind each: the same member seen from two kurins is two
        // entries, each placed by its own membership. Still one SQL — the join replaces the old
        // WHERE on the member's own kurin, and nothing else about the shape changed. No Include
        // graph: scalars come from the root query, UserRole is a correlated subquery over
        // Identity (replacing the old per-list GroupJoin), Address/School are masked in SQL
        // from the caller's visibility, and only active leadership/warnings are pulled.
        private async Task<IEnumerable<MemberListItemDto>> ProjectListItemsAsync(
            IQueryable<Membership> memberships,
            MemberFieldVisibility visibility,
            CancellationToken cancellationToken)
        {
            var canSeeAll = visibility.CanSeeAllPrivate;
            var currentUserId = visibility.CurrentUserId;
            var visibleGroupKeys = visibility.VisibleGroupKeys as IReadOnlyCollection<Guid> ?? visibility.VisibleGroupKeys.ToList();

            var source = from ms in memberships
                         join person in Context.Members on ms.MemberKey equals person.MemberKey
                         select new { Placement = ms, Person = person };

            var rows = await source
                .Select(x => new
                {
                    // The office they hold *in this kurin*. It used to come from the identity store,
                    // which said what someone was anywhere and so showed a виховник's office to a
                    // kurin they are only a member of. Read as its two halves and named afterwards:
                    // the enums are stored as numbers, so the name cannot be assembled in SQL.
                    Office = (from history in Context.LeadershipHistories
                              where history.MemberKey == x.Person.MemberKey && history.EndDate == null
                              join office in Context.Leaderships
                                  on history.LeadershipKey equals office.LeadershipKey
                              where office.EndDate == null
                                  && (office.KurinKey == x.Placement.KurinKey
                                      || (office.GroupKey != null && office.Group!.KurinKey == x.Placement.KurinKey))
                              orderby office.Type, history.Role
                              select new { office.Type, history.Role }).FirstOrDefault(),
                    Item = new MemberListItemDto
                {
                    MemberKey = x.Person.MemberKey,
                    GroupKey = x.Placement.GroupKey,
                    KurinKey = x.Placement.KurinKey,
                    UserKey = x.Person.UserKey,
                    FirstName = x.Person.FirstName,
                    MiddleName = x.Person.MiddleName,
                    LastName = x.Person.LastName,
                    Email = x.Person.Email,
                    PhoneNumber = x.Person.PhoneNumber,
                    DateOfBirth = x.Person.DateOfBirth,
                    Address = (canSeeAll
                        || (x.Person.UserKey != null && x.Person.UserKey == currentUserId)
                        || (x.Placement.GroupKey != null && visibleGroupKeys.Contains(x.Placement.GroupKey.Value)))
                        ? x.Person.Address : null,
                    School = (canSeeAll
                        || (x.Person.UserKey != null && x.Person.UserKey == currentUserId)
                        || (x.Placement.GroupKey != null && visibleGroupKeys.Contains(x.Placement.GroupKey.Value)))
                        ? x.Person.School : null,
                    // Mirror the Member -> MemberResponse mapping: newest history level, else the stored one.
                    LatestPlastLevel = x.Person.PlastLevelHistory
                        .OrderByDescending(history => history.DateAchieved)
                        .Select(history => (PlastLevel?)history.PlastLevel)
                        .FirstOrDefault() ?? x.Person.LatestPlastLevel,
                    ProfilePhotoBlobName = x.Person.ProfilePhotoBlobName,
                    ProfileVerificationStatus = x.Person.ProfileVerificationStatus,
                    ProfileVerifiedAtUtc = x.Person.ProfileVerifiedAtUtc,
                    ProfileVerifiedByUserKey = x.Person.ProfileVerifiedByUserKey,
                    ProfileVerificationNote = x.Person.ProfileVerificationNote,
                    LeadershipHistories = x.Person.LeadershipHistories
                        .Where(h => h.EndDate == null)
                        .Select(h => new LeadershipHistoryDto
                        {
                            LeadershipHistoryKey = h.LeadershipHistoryKey,
                            MemberKey = h.MemberKey,
                            LeadershipKey = h.LeadershipKey,
                            Role = h.Role,
                            LeadershipType = h.Leadership.Type,
                            GroupName = h.Leadership.Group != null ? h.Leadership.Group.Name : null,
                            StartDate = h.StartDate,
                            EndDate = h.EndDate
                        }).ToList(),
                    Warnings = x.Person.MemberWarnings
                        .Where(w => w.RevokedAtUtc == null)
                        .Select(w => new MemberWarningDto
                        {
                            MemberWarningKey = w.MemberWarningKey,
                            MemberKey = w.MemberKey,
                            Level = w.Level,
                            IssuedAtUtc = w.IssuedAtUtc,
                            ExpiresAtUtc = w.ExpiresAtUtc,
                            IssuedByUserKey = w.IssuedByUserKey,
                            RevokedByUserKey = w.RevokedByUserKey,
                            RevokedAtUtc = w.RevokedAtUtc
                        }).ToList()
                    }
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            foreach (var row in rows.Where(row => row.Office is not null))
            {
                row.Item.UserRole = SystemRole.ForOffice(row.Office!.Type, row.Office.Role);
            }

            return [.. rows.Select(row => row.Item)];
        }

        public Task<Guid?> GetUserKeyByMemberAsync(Guid memberKey, CancellationToken cancellationToken = default)
            => Context.Members
                .Where(m => m.MemberKey == memberKey)
                .Select(m => m.UserKey)
                .FirstOrDefaultAsync(cancellationToken);

        /// <summary>
        /// People as another module sees them, each placed by their most recent open membership.
        /// Someone who belongs to no kurin — an account activated but not yet joined to anything —
        /// still answers, with an empty kurin, because the caller asked about the person.
        /// </summary>
        private IQueryable<MemberSummary> SummariesOf(IQueryable<Member> people)
            => from m in people
               from ms in ActiveMemberships
                   .Where(candidate => candidate.MemberKey == m.MemberKey)
                   .OrderByDescending(candidate => candidate.JoinedAtUtc)
                   .Take(1)
                   .DefaultIfEmpty()
               select new MemberSummary(
                   m.MemberKey,
                   m.UserKey,
                   ms == null ? Guid.Empty : ms.KurinKey,
                   ms == null ? (Guid?)null : ms.GroupKey,
                   m.FirstName,
                   m.LastName,
                   m.Email,
                   m.ProfilePhotoBlobName);

        public Task<MemberSummary?> GetSummaryByKeyAsync(Guid memberKey, CancellationToken cancellationToken = default)
            => SummariesOf(Context.Members.Where(m => m.MemberKey == memberKey))
                .FirstOrDefaultAsync(cancellationToken);

        public Task<MemberSummary?> GetSummaryByUserKeyAsync(Guid userKey, CancellationToken cancellationToken = default)
            => SummariesOf(Context.Members.Where(m => m.UserKey == userKey))
                .FirstOrDefaultAsync(cancellationToken);

        // Asked with a kurin in hand, so the placement is that kurin's membership and not whichever
        // one happens to be newest — the same person read from another kurin answers differently.
        public async Task<IReadOnlyCollection<MemberSummary>> GetSummariesByKurinKeyAsync(Guid kurinKey, CancellationToken cancellationToken = default)
            => await (from ms in ActiveMemberships.Where(ms => ms.KurinKey == kurinKey)
                      join m in Context.Members on ms.MemberKey equals m.MemberKey
                      select new MemberSummary(
                          m.MemberKey,
                          m.UserKey,
                          ms.KurinKey,
                          ms.GroupKey,
                          m.FirstName,
                          m.LastName,
                          m.Email,
                          m.ProfilePhotoBlobName))
                .ToListAsync(cancellationToken);

        public async Task<IReadOnlyCollection<MemberSummary>> GetAllSummariesAsync(CancellationToken cancellationToken = default)
            => await SummariesOf(Context.Members).ToListAsync(cancellationToken);

        public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
            => Context.Members.AnyAsync(m => m.Email == email, cancellationToken);

        public Task<MemberAccountLink?> GetAccountLinkAsync(Guid memberKey, CancellationToken cancellationToken = default)
            => Context.Members
                .Where(m => m.MemberKey == memberKey)
                .Select(m => new MemberAccountLink(m.MemberKey, m.UserKey))
                .FirstOrDefaultAsync(cancellationToken);

        public Task<Guid?> GetKurinKeyByMemberAsync(Guid memberKey, CancellationToken cancellationToken = default)
            => ActiveMemberships
                .Where(ms => ms.MemberKey == memberKey)
                .OrderByDescending(ms => ms.JoinedAtUtc)
                .Select(ms => (Guid?)ms.KurinKey)
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<IEnumerable<MemberLookupDto>> GetMentorCandidatesLookupAsync(Guid kurinKey, CancellationToken cancellationToken = default)
        {
            // One row per candidate, with whichever office they hold in this kurin. It used to fan out
            // over the identity store's roles and collapse them again, which also meant showing an
            // office earned in a different kurin.
            var rows = await PeopleOf(ActiveMemberships.Where(ms => ms.KurinKey == kurinKey))
                .Where(m => m.UserKey != null)
                .Select(m => new
                {
                    Office = (from history in Context.LeadershipHistories
                              where history.MemberKey == m.MemberKey && history.EndDate == null
                              join office in Context.Leaderships
                                  on history.LeadershipKey equals office.LeadershipKey
                              where office.EndDate == null
                                  && (office.KurinKey == kurinKey
                                      || (office.GroupKey != null && office.Group!.KurinKey == kurinKey))
                              orderby office.Type, history.Role
                              select new { office.Type, history.Role }).FirstOrDefault(),
                    Item = new MemberLookupDto
                    {
                        MemberKey = m.MemberKey,
                        UserKey = m.UserKey,
                        FirstName = m.FirstName,
                        MiddleName = m.MiddleName,
                        LastName = m.LastName
                    }
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            foreach (var row in rows.Where(row => row.Office is not null))
            {
                row.Item.UserRole = SystemRole.ForOffice(row.Office!.Type, row.Office.Role);
            }

            return [.. rows.Select(row => row.Item)];
        }

        public async Task<Member?> GetByUserKeyAsync(Guid userKey, CancellationToken cancellationToken = default)
        {
            return await Context.Members
                .Include(m => m.Group)
                .Include(m => m.Kurin)
                .Include(m => m.MemberWarnings)
                .Include(m => m.MemberAwards)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.UserKey == userKey, cancellationToken);
        }

        public async Task<Member?> GetTrackedByUserKeyAsync(Guid userKey, CancellationToken cancellationToken = default)
        {
            return await Context.Members
                .FirstOrDefaultAsync(m => m.UserKey == userKey, cancellationToken);
        }

        public async Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        {
            return await Context.Members
                .FirstOrDefaultAsync(m => m.Email == email, cancellationToken);
        }

        public override Task<IEnumerable<Member>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Use GetAllAsync(Guid groupKey, CancellationToken token) or GetAllByKurinkey(...) instead.");
        }

        public override void Update(Member member, CancellationToken cancellationToken = default)
        {
            Context.Members.Update(member);
        }

        #region PlastLevelHistory Methods

        #endregion

        #region LeadershipHistory Methods

        #endregion
    }
}
