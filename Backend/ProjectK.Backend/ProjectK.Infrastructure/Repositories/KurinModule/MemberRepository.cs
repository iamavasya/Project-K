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

            return await source
                .Select(x => new MemberListItemDto
                {
                    MemberKey = x.Person.MemberKey,
                    GroupKey = x.Placement.GroupKey,
                    KurinKey = x.Placement.KurinKey,
                    UserKey = x.Person.UserKey,
                    // Everyone carries the baseline Member role, so taking whatever the store returned
                    // first often hid the office. Skip the baseline and order so the result is stable.
                    // A single field still cannot express a member holding several offices — see the
                    // role-system unification work.
                    UserRole = (from ur in Context.UserRoles
                                where x.Person.UserKey != null && ur.UserId == x.Person.UserKey
                                join r in Context.Roles on ur.RoleId equals r.Id
                                where r.Name != SystemRole.Member
                                orderby r.Name
                                select r.Name).FirstOrDefault(),
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
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
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
            // Join fans out to one row per (member, role); a member now holds several roles (Member plus
            // office roles), so collapse to one row per member and prefer a non-baseline role for display.
            var rows = await PeopleOf(ActiveMemberships.Where(ms => ms.KurinKey == kurinKey))
                .Where(m => m.UserKey != null)
                .GroupJoin(Context.UserRoles, m => m.UserKey, ur => (Guid?)ur.UserId, (member, userRoles) => new { member, userRoles })
                .SelectMany(x => x.userRoles.DefaultIfEmpty(), (x, userRole) => new { x.member, userRole })
                .GroupJoin(Context.Roles, x => x.userRole != null ? (Guid?)x.userRole.RoleId : null, role => (Guid?)role.Id, (x, roles) => new { x.member, roles })
                .SelectMany(x => x.roles.DefaultIfEmpty(), (x, role) => new { x.member, role })
                .Select(m => new MemberLookupDto
                {
                    MemberKey = m.member.MemberKey,
                    UserKey = m.member.UserKey,
                    FirstName = m.member.FirstName,
                    MiddleName = m.member.MiddleName,
                    LastName = m.member.LastName,
                    UserRole = m.role != null ? m.role.Name : null
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            return rows
                .GroupBy(row => row.MemberKey)
                .Select(group => group
                    .OrderBy(row => string.IsNullOrEmpty(row.UserRole)
                        || string.Equals(row.UserRole, SystemRole.Member, StringComparison.OrdinalIgnoreCase) ? 1 : 0)
                    .First())
                .ToList();
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
