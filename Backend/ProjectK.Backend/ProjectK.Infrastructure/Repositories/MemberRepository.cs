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

namespace ProjectK.Infrastructure.Repositories
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

        public async Task<IEnumerable<Member>> GetAllAsync(Guid groupKey, CancellationToken cancellationToken = default)
        {
            return await Context.Members.Where(m => m.GroupKey == groupKey)
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

        public Task<IEnumerable<ProjectK.Common.Models.Dtos.MemberListItemDto>> GetListItemsByKurinKeyAsync(Guid kurinKey, ProjectK.Common.Models.Dtos.MemberFieldVisibility visibility, CancellationToken cancellationToken = default)
            => ProjectListItemsAsync(Context.Members.Where(m => m.KurinKey == kurinKey), visibility, cancellationToken);

        public Task<IEnumerable<ProjectK.Common.Models.Dtos.MemberListItemDto>> GetListItemsByGroupKeyAsync(Guid groupKey, ProjectK.Common.Models.Dtos.MemberFieldVisibility visibility, CancellationToken cancellationToken = default)
            => ProjectListItemsAsync(Context.Members.Where(m => m.GroupKey == groupKey), visibility, cancellationToken);

        // Single projection shared by the kurin- and group-scoped list reads. No Include
        // graph: scalars come from the root query, UserRole is a correlated subquery over
        // Identity (replacing the old per-list GroupJoin), Address/School are masked in SQL
        // from the caller's visibility, and only active leadership/warnings are pulled.
        private async Task<IEnumerable<ProjectK.Common.Models.Dtos.MemberListItemDto>> ProjectListItemsAsync(
            IQueryable<Member> source,
            ProjectK.Common.Models.Dtos.MemberFieldVisibility visibility,
            CancellationToken cancellationToken)
        {
            var canSeeAll = visibility.CanSeeAllPrivate;
            var currentUserId = visibility.CurrentUserId;
            var visibleGroupKeys = visibility.VisibleGroupKeys as IReadOnlyCollection<Guid> ?? visibility.VisibleGroupKeys.ToList();

            return await source
                .Select(m => new ProjectK.Common.Models.Dtos.MemberListItemDto
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
                        .Select(h => new ProjectK.Common.Models.Dtos.LeadershipHistoryDto
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
                        .Select(w => new ProjectK.Common.Models.Dtos.MemberWarningDto
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

        public async Task<IEnumerable<ProjectK.Common.Models.Dtos.MemberLookupDto>> GetMentorCandidatesLookupAsync(Guid kurinKey, CancellationToken cancellationToken = default)
        {
            // Join fans out to one row per (member, role); a member now holds several roles (Member plus
            // office roles), so collapse to one row per member and prefer a non-baseline role for display.
            var rows = await Context.Members
                .Where(m => m.KurinKey == kurinKey && m.UserKey != null)
                .GroupJoin(Context.UserRoles, m => m.UserKey, ur => (Guid?)ur.UserId, (member, userRoles) => new { member, userRoles })
                .SelectMany(x => x.userRoles.DefaultIfEmpty(), (x, userRole) => new { x.member, userRole })
                .GroupJoin(Context.Roles, x => x.userRole != null ? (Guid?)x.userRole.RoleId : null, role => (Guid?)role.Id, (x, roles) => new { x.member, roles })
                .SelectMany(x => x.roles.DefaultIfEmpty(), (x, role) => new { x.member, role })
                .Select(m => new ProjectK.Common.Models.Dtos.MemberLookupDto
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
        public async Task AddPlastLevelHistoryAsync(Guid memberKey, PlastLevelHistory history, CancellationToken cancellationToken = default)
        {
            var member = await Context.Members
                .Include(m => m.PlastLevelHistory)
                .FirstOrDefaultAsync(m => m.MemberKey == memberKey, cancellationToken);

            if (member == null) throw new ArgumentNullException(memberMessage);

            member.PlastLevelHistory.Add(history);

            // Updating LatestPlastLevel based on most recent achievement date
            var lastHistory = member.PlastLevelHistory
                .OrderByDescending(h => h.DateAchieved)
                .FirstOrDefault();
            member.LatestPlastLevel = lastHistory?.PlastLevel;
        }

        public async Task RemovePlastLevelHistoryAsync(Guid memberKey, Guid historyKey, CancellationToken cancellationToken = default)
        {
            var member = await Context.Members
                .Include(m => m.PlastLevelHistory)
                .FirstOrDefaultAsync(m => m.MemberKey == memberKey, cancellationToken);

            if (member == null) throw new ArgumentNullException(memberMessage);

            var history = member.PlastLevelHistory.FirstOrDefault(h => h.PlastLevelHistoryKey == historyKey);
            if (history != null)
            {
                member.PlastLevelHistory.Remove(history);

                // Update LatestPlastLevel
                var last = member.PlastLevelHistory.OrderByDescending(h => h.DateAchieved).FirstOrDefault();
                member.LatestPlastLevel = last?.PlastLevel;
            }
        }

        public async Task<IEnumerable<PlastLevelHistory>> GetPlastLevelHistoryAsync(Guid memberKey, CancellationToken cancellationToken = default)
        {
            var member = await Context.Members
                .Include(m => m.PlastLevelHistory)
                .FirstOrDefaultAsync(m => m.MemberKey == memberKey, cancellationToken);

            return member?.PlastLevelHistory ?? Enumerable.Empty<PlastLevelHistory>();
        }

        public async Task UpdatePlastLevelHistoryAsync(Guid memberKey, PlastLevelHistory updatedHistory, CancellationToken cancellationToken = default)
        {
            var member = await Context.Members
                .Include(m => m.PlastLevelHistory)
                .FirstOrDefaultAsync(m => m.MemberKey == memberKey, cancellationToken);

            if (member == null)
                throw new ArgumentNullException(memberMessage);

            var history = member.PlastLevelHistory
                .FirstOrDefault(h => h.PlastLevelHistoryKey == updatedHistory.PlastLevelHistoryKey);

            if (history == null)
                throw new InvalidOperationException($"Plast level history '{updatedHistory.PlastLevelHistoryKey}' not found for member '{memberKey}'.");

            // Updating fields
            history.PlastLevel = updatedHistory.PlastLevel;
            history.DateAchieved = updatedHistory.DateAchieved;

            // Updating LatestPlastLevel
            var lastHistory = member.PlastLevelHistory
                .OrderByDescending(h => h.DateAchieved)
                .FirstOrDefault();

            member.LatestPlastLevel = lastHistory?.PlastLevel;
        }
        #endregion

        #region LeadershipHistory Methods

        public async Task AddLeadershipHistoryAsync(Guid memberKey, LeadershipHistory history, CancellationToken cancellationToken)
        {
            var member = await Context.Members
                .Include(m => m.LeadershipHistories)
                .FirstOrDefaultAsync(m => m.MemberKey == memberKey, cancellationToken);

            if (member == null) throw new ArgumentNullException(memberMessage);

            member.LeadershipHistories.Add(history);
        }

        public async Task EndLeadershipAsync(Guid memberKey, Guid historyKey, DateOnly endDate, CancellationToken cancellationToken)
        {
            var member = await Context.Members
                .Include(m => m.LeadershipHistories)
                .FirstOrDefaultAsync(m => m.MemberKey == memberKey, cancellationToken);

            if (member == null) throw new ArgumentNullException(memberMessage);

            var history = member.LeadershipHistories.FirstOrDefault(h => h.LeadershipHistoryKey == historyKey);

            if (history != null) history.EndDate = endDate;
        }

        public async Task RemoveLeadershipHistoryAsync(Guid memberKey, Guid historyKey, CancellationToken cancellationToken)
        {
            var member = await Context.Members
                .Include(m => m.LeadershipHistories)
                .FirstOrDefaultAsync(m => m.MemberKey == memberKey, cancellationToken);
            if (member == null) throw new ArgumentNullException(memberMessage);
            var history = member.LeadershipHistories.FirstOrDefault(h => h.LeadershipHistoryKey == historyKey);
            if (history != null)
            {
                member.LeadershipHistories.Remove(history);
            }
        }

        public async Task UpdateLeadershipHistoryAsync(
            Guid memberKey,
            LeadershipHistory updatedHistory,
            CancellationToken cancellationToken)
        {
            var member = await Context.Members
                .Include(m => m.LeadershipHistories)
                .FirstOrDefaultAsync(m => m.MemberKey == memberKey, cancellationToken);

            if (member == null)
                throw new ArgumentNullException($"Member with key '{memberKey}' not found.");

            var existingHistory = member.LeadershipHistories
                .FirstOrDefault(h => h.LeadershipHistoryKey == updatedHistory.LeadershipHistoryKey);

            if (existingHistory == null)
                throw new ArgumentNullException($"Leadership history '{updatedHistory.LeadershipHistoryKey}' not found for member '{memberKey}'.");

            existingHistory.Role = updatedHistory.Role;
            existingHistory.StartDate = updatedHistory.StartDate;
            existingHistory.EndDate = updatedHistory.EndDate;
            existingHistory.LeadershipKey = updatedHistory.LeadershipKey;
        }
        #endregion
    }
}
