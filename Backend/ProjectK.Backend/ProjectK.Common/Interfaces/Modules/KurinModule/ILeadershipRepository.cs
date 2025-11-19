using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces.Modules.KurinModule
{
    public interface ILeadershipRepository
    {
        Task<Leadership?> GetByKeyAsync(Guid leadershipKey, CancellationToken cancellationToken = default);
        Task<IEnumerable<Leadership>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IEnumerable<Leadership>> GetAllByTypeAsync(LeadershipType type, Guid entityKey,  CancellationToken cancellationToken = default);

        void Add(Leadership leadership, CancellationToken cancellationToken = default);
        void Update(Leadership updatedLeadership, CancellationToken cancellationToken = default);
        Task CloseLeadershipAsync(Guid leadershipKey, DateOnly endDate, CancellationToken cancellationToken = default);

        Task<IEnumerable<LeadershipHistory>> GetLeadershipHistoriesAsync(Guid leadershipKey, CancellationToken cancellationToken = default);
        void LeadershipHistoriesRemoveRange(IEnumerable<LeadershipHistory> histories);
    }
}
