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
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<PlanningSession>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PlanningSession?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public void Update(PlanningSession entity, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
