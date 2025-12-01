using ProjectK.Common.Entities.KurinModule.Planning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces.Modules.KurinModule
{
    public interface IPlanningSessionRepository : IBaseEntityRepository<PlanningSession>
    {
        Task<PlanningSession?> GetByKeyWithDetailsAsync(Guid entityKey, CancellationToken cancellationToken = default);
        Task<IEnumerable<PlanningSession>> GetAllByKurinKeyAsync(Guid kurinKey, CancellationToken cancellationToken = default);
    }
}
