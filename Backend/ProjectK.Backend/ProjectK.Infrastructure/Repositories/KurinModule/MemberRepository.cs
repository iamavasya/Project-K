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

namespace ProjectK.Infrastructure.Repositories.KurinModule
{
    public class MemberRepository : BaseEntityRepository<Member>, IMemberRepository
    {
        private readonly string memberMessage = "Member not found.";
        public MemberRepository(AppDbContext context) : base(context)
        {
        }

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
            return await Context.Members
                                .Where(m => m.GroupKey == groupKey)
                                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Member>> GetAllByKurinKeyAsync(Guid kurinKey, CancellationToken cancellationToken = default)
        {
            return await Context.Members.Where(m => m.KurinKey == kurinKey)
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
            => ProjectListItemsAsync(Context.Members.Where(m => m.KurinKey == kurinKey), visibility, cancellationToken);

        public Task<IEnumerable<MemberListItemDto>> GetListItemsByGroupKeyAsync(Guid groupKey, MemberFieldVisibility visibility, CancellationToken cancellationToken = default)
            => ProjectListItemsAsync(Context.Members.Where(m => m.GroupKey == groupKey), visibility, cancellationToken);

        // Single projection shared by the kurin- and group-scoped list reads. No Include
        // graph: scalars come from the root query, UserRole is a correlated subquery over
        // Identity (replacing the old per-list GroupJoin), Address/School are masked in SQL
        // from the caller's visibility, and only active leadership/warnings are pulled.
        private async Task<IEnumerable<MemberListItemDto>> ProjectListItemsAsync(
            IQueryable<Member> source,
            MemberFieldVisibility visibility,
            CancellationToken cancellationToken)
        {
            var canSeeAll = visibility.CanSeeAllPrivate;
            var currentUserId = visibility.CurrentUserId;
            var visibleGroupKeys = visibility.VisibleGroupKeys as IReadOnlyCollection<Guid> ?? visibility.VisibleGroupKeys.ToList();

            return await source
                .Select(m => new MemberListItemDto
                {
                    MemberKey = m.MemberKey,
                    GroupKey = m.GroupKey,
                    KurinKey = m.KurinKey,
                    UserKey = m.UserKey,
                    // Everyone carries the baseline Member role, so taking whatever the store returned
                    // first often hid the office. Skip the baseline and order so the result is stable.
                    // A single field still cannot express a member holding several offices — see the
                    // role-system unification work.
                    UserRole = (from ur in Context.UserRoles
                                where m.UserKey != null && ur.UserId == m.UserKey
                                join r in Context.Roles on ur.RoleId equals r.Id
                                where r.Name != SystemRole.Member
                                orderby r.Name
                                select r.Name).FirstOrDefault(),
                    FirstName = m.FirstName,
                    MiddleName = m.MiddleName,
                    LastName = m.LastName,
                    Email = m.Email,
                    PhoneNumber = m.PhoneNumber,
                    DateOfBirth = m.DateOfBirth,
                    Address = (canSeeAll
                        || (m.UserKey != null && m.UserKey == currentUserId)
                        || (m.GroupKey != null && visibleGroupKeys.Contains(m.GroupKey.Value)))
                        ? m.Address : null,
                    School = (canSeeAll
                        || (m.UserKey != null && m.UserKey == currentUserId)
                        || (m.GroupKey != null && visibleGroupKeys.Contains(m.GroupKey.Value)))
                        ? m.School : null,
                    // Mirror the Member -> MemberResponse mapping: newest history level, else the stored one.
                    LatestPlastLevel = m.PlastLevelHistory
                        .OrderByDescending(history => history.DateAchieved)
                        .Select(history => (PlastLevel?)history.PlastLevel)
                        .FirstOrDefault() ?? m.LatestPlastLevel,
                    ProfilePhotoBlobName = m.ProfilePhotoBlobName,
                    ProfileVerificationStatus = m.ProfileVerificationStatus,
                    ProfileVerifiedAtUtc = m.ProfileVerifiedAtUtc,
                    ProfileVerifiedByUserKey = m.ProfileVerifiedByUserKey,
                    ProfileVerificationNote = m.ProfileVerificationNote,
                    LeadershipHistories = m.LeadershipHistories
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
                    Warnings = m.MemberWarnings
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

        public Task<Guid?> GetKurinKeyByMemberAsync(Guid memberKey, CancellationToken cancellationToken = default)
            => Context.Members
                .Where(m => m.MemberKey == memberKey)
                .Select(m => (Guid?)m.KurinKey)
                .FirstOrDefaultAsync(cancellationToken);

        public async Task<IEnumerable<MemberLookupDto>> GetMentorCandidatesLookupAsync(Guid kurinKey, CancellationToken cancellationToken = default)
        {
            // Join fans out to one row per (member, role); a member now holds several roles (Member plus
            // office roles), so collapse to one row per member and prefer a non-baseline role for display.
            var rows = await Context.Members
                .Where(m => m.KurinKey == kurinKey && m.UserKey != null)
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
