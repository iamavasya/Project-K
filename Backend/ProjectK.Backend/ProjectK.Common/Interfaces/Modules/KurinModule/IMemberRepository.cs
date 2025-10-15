using Microsoft.EntityFrameworkCore.ChangeTracking;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Entities.KurinModule.Leadership;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces.Modules.KurinModule
{
    public interface IMemberRepository : IBaseEntityRepository<Member>
    {
        Task<IEnumerable<Member>> GetAllAsync(Guid groupKey, CancellationToken token = default);
        Task<IEnumerable<Member>> GetAllByKurinKeyAsync(Guid kurinKey, CancellationToken token = default);

        #region PlastLevelHistory Methods
        Task AddPlastLevelHistoryAsync(Guid memberKey, PlastLevelHistory history, CancellationToken token = default);
        Task UpdatePlastLevelHistoryAsync(Guid memberKey, PlastLevelHistory updatedHistory, CancellationToken token = default);
        Task RemovePlastLevelHistoryAsync(Guid memberKey, Guid historyKey, CancellationToken token = default);
        Task<IEnumerable<PlastLevelHistory>> GetPlastLevelHistoryAsync(Guid memberKey, CancellationToken token = default);
        #endregion

        #region LeadershipHistory Methods

        Task AddLeadershipHistoryAsync(Guid memberKey, LeadershipHistory history, CancellationToken cancellationToken);
        Task EndLeadershipAsync(Guid memberKey, Guid historyKey, DateOnly endDate, CancellationToken cancellationToken);
        Task RemoveLeadershipHistoryAsync(Guid memberKey, Guid historyKey, CancellationToken cancellationToken);
        Task UpdateLeadershipHistoryAsync(Guid memberKey, LeadershipHistory updatedHistory, CancellationToken cancellationToken);

        #endregion
    }
}
