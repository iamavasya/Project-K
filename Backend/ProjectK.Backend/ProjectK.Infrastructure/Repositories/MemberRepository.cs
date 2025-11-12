using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Infrastructure.DbContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Repositories
{
    public class MemberRepository : IMemberRepository
    {
        private readonly AppDbContext _context;
        public MemberRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Create(Member member, CancellationToken token = default)
        {
            _context.Members.Add(member);
        }

        public void Delete(Member member, CancellationToken token = default)
        {
            _context.Members.Remove(member);
        }

        public async Task<Member?> GetByKeyAsync(Guid entityKey, CancellationToken token = default)
        {
            return await _context.Members.Include(m => m.Group)
                                         .Include(m => m.Kurin)
                                         .Include(m => m.PlastLevelHistory)
                                         .FirstOrDefaultAsync(e => e.MemberKey == entityKey, token);
        }

        public async Task<IEnumerable<Member>> GetAllAsync(Guid groupKey, CancellationToken token = default)
        {
            return await _context.Members.Where(m => m.GroupKey == groupKey)
                                         .Include(m => m.Group)
                                         .Include(m => m.Kurin)
                                         .AsNoTracking()
                                         .ToListAsync(token);
        }

        public async Task<IEnumerable<Member>> GetAllByKurinKeyAsync(Guid kurinKey, CancellationToken token = default)
        {
            return await _context.Members.Where(m => m.KurinKey == kurinKey)
                                         .Include(m => m.Group)
                                         .Include(m => m.Kurin)
                                         .AsNoTracking()
                                         .ToListAsync(token);
        }

        public Task<IEnumerable<Member>> GetAllAsync(CancellationToken token = default)
        {
            throw new NotSupportedException("Use GetAllAsync(Guid groupKey, CancellationToken token) or GetAllByKurinkey(...) instead.");
        }

        public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken token = default)
        {
            return await _context.Members.AnyAsync(e => e.MemberKey == entityKey, token);
        }

        public void Update(Member member, CancellationToken token = default)
        {
            _context.Members.Update(member);
        }

        #region PlastLevelHistory Methods
        public async Task AddPlastLevelHistoryAsync(Guid memberKey, PlastLevelHistory history, CancellationToken cancellationToken = default)
        {
            var member = await _context.Members
                .Include(m => m.PlastLevelHistory)
                .FirstOrDefaultAsync(m => m.MemberKey == memberKey, cancellationToken);

            if (member == null) throw new Exception("Member not found.");

            member.PlastLevelHistory.Add(history);

            // Updating LatestPlastLevel
            if (member.LatestPlastLevel == null || history.PlastLevel > member.LatestPlastLevel)
            {
                member.LatestPlastLevel = history.PlastLevel;
            }
        }

        public async Task RemovePlastLevelHistoryAsync(Guid memberKey, Guid historyKey, CancellationToken cancellationToken)
        {
            var member = await _context.Members
                .Include(m => m.PlastLevelHistory)
                .FirstOrDefaultAsync(m => m.MemberKey == memberKey, cancellationToken);

            if (member == null) throw new Exception("Member not found");

            var history = member.PlastLevelHistory.FirstOrDefault(h => h.PlastLevelHistoryKey == historyKey);
            if (history != null)
            {
                member.PlastLevelHistory.Remove(history);

                // Update LatestPlastLevel
                var last = member.PlastLevelHistory.OrderByDescending(h => h.DateAchieved).FirstOrDefault();
                member.LatestPlastLevel = last?.PlastLevel;
            }
        }

        public async Task<IEnumerable<PlastLevelHistory>> GetPlastLevelHistoryAsync(Guid memberKey, CancellationToken cancellationToken)
        {
            var member = await _context.Members
                .Include(m => m.PlastLevelHistory)
                .FirstOrDefaultAsync(m => m.MemberKey == memberKey, cancellationToken);

            return member?.PlastLevelHistory ?? Enumerable.Empty<PlastLevelHistory>();
        }

        public async Task UpdatePlastLevelHistoryAsync(Guid memberKey, PlastLevelHistory updatedHistory, CancellationToken cancellationToken)
        {
            var member = await _context.Members
                .Include(m => m.PlastLevelHistory)
                .FirstOrDefaultAsync(m => m.MemberKey == memberKey, cancellationToken);

            if (member == null)
                throw new Exception("Member not found");

            var history = member.PlastLevelHistory
                .FirstOrDefault(h => h.PlastLevelHistoryKey == updatedHistory.PlastLevelHistoryKey);

            if (history == null)
                throw new Exception("PlastLevelHistory not found");

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

        public async Task AddLeadershipHistoryAsync(Guid memberKey, LeadershipHistory leadershipHistory, CancellationToken cancellationToken)
        {
            var member = await _context.Members
                .Include(m => m.LeadershipHistories)
                .FirstOrDefaultAsync(m => m.MemberKey == memberKey, cancellationToken);

            if (member == null) throw new Exception("Member not found");

            member.LeadershipHistories.Add(leadershipHistory);
        }

        public async Task EndLeadershipAsync(Guid memberKey, Guid historyKey, DateOnly endDate, CancellationToken cancellationToken)
        {
            var member = await _context.Members
                .Include(m => m.LeadershipHistories)
                .FirstOrDefaultAsync(m => m.MemberKey == memberKey, cancellationToken);

            if (member == null) throw new Exception("Member not found");

            var history = member.LeadershipHistories.FirstOrDefault(h => h.LeadershipHistoryKey == historyKey);
            
            if (history != null) history.EndDate = endDate;
        }

        public async Task RemoveLeadershipHistoryAsync(Guid memberKey, Guid historyKey, CancellationToken cancellationToken)
        {
            var member = await _context.Members
                .Include(m => m.LeadershipHistories)
                .FirstOrDefaultAsync(m => m.MemberKey == memberKey, cancellationToken);
            if (member == null) throw new Exception("Member not found");
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
            var member = await _context.Members
                .Include(m => m.LeadershipHistories)
                .FirstOrDefaultAsync(m => m.MemberKey == memberKey, cancellationToken);

            if (member == null)
                throw new KeyNotFoundException($"Member with key '{memberKey}' not found.");

            var existingHistory = member.LeadershipHistories
                .FirstOrDefault(h => h.LeadershipHistoryKey == updatedHistory.LeadershipHistoryKey);

            if (existingHistory == null)
                throw new KeyNotFoundException($"Leadership history '{updatedHistory.LeadershipHistoryKey}' not found for member '{memberKey}'.");

            existingHistory.Role = updatedHistory.Role;
            existingHistory.StartDate = updatedHistory.StartDate;
            existingHistory.EndDate = updatedHistory.EndDate;
            existingHistory.LeadershipKey = updatedHistory.LeadershipKey;
        }
        #endregion
    }
}
