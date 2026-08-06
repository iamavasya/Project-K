using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.KurinModule.Planning;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using ProjectK.Infrastructure.DbContexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Repositories
{
    public class PlanningSessionRepository : IPlanningSessionRepository
    {
        private readonly AppDbContext _context;
        public PlanningSessionRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Create(PlanningSession entity, CancellationToken cancellationToken = default)
        {
            _context.PlanningSessions.Add(entity);
        }

        public void Delete(PlanningSession entity, CancellationToken cancellationToken = default)
        {
            _context.PlanningSessions.Remove(entity);
        }

        public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.PlanningSessions.AnyAsync(ps => ps.PlanningSessionKey == entityKey, cancellationToken);
        }

        public Task<IEnumerable<PlanningSession>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Use GetAllByKurinKeyAsync(Guid kurinKey, CancellationToken token) instead.");
        }

        public async Task<PlanningSession?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.PlanningSessions.FirstOrDefaultAsync(ps => ps.PlanningSessionKey == entityKey, cancellationToken);
        }

        public void Update(PlanningSession entity, CancellationToken cancellationToken = default)
        {
            _context.PlanningSessions.Update(entity);
        }

        public async Task<PlanningSession?> GetByKeyWithDetailsAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.PlanningSessions
                                 .Include(ps => ps.Participants)
                                    .ThenInclude(p => p.BusyRanges)
                                 .FirstOrDefaultAsync(ps => ps.PlanningSessionKey == entityKey, cancellationToken);
        }

        public async Task<IEnumerable<PlanningSession>> GetAllByKurinKeyAsync(Guid kurinKey, CancellationToken cancellationToken = default)
        {
            return await _context.PlanningSessions
                                 .Where(ps => ps.KurinKey == kurinKey)
                                 .AsNoTracking()
                                 .ToListAsync(cancellationToken);
        }
    }
}
