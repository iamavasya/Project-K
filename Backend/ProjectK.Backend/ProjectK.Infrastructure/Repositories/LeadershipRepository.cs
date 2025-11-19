using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Infrastructure.DbContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Repositories
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

        public async Task CloseLeadershipAsync(Guid leadershipKey, DateOnly endDate, CancellationToken cancellationToken = default)
        {
            // Винести в бізнес логіку
            // Викликати тільки Update
            throw new NotSupportedException();
            var leadership = await _context.Leaderships.FindAsync(leadershipKey, cancellationToken);
            if (leadership != null)
            {
                leadership.EndDate = endDate;
                _context.Leaderships.Update(leadership);
            }
            else
            {
                throw new Exception("Leadership not found");
            }
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
    }
}
