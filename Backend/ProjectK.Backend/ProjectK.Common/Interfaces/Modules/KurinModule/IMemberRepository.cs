using Microsoft.EntityFrameworkCore.ChangeTracking;
using ProjectK.Common.Entities.KurinModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces.Modules.KurinModule
{
    public interface IMemberRepository : IBaseEntityRepository<Member>
    {
        Task<IEnumerable<Member>> GetAllAsync(Guid groupKey, CancellationToken cancellationToken = default);
        Task<IEnumerable<Member>> GetAllByKurinKeyAsync(Guid kurinKey, CancellationToken cancellationToken = default);
        Task<Member?> GetByUserKeyAsync(Guid userKey, CancellationToken cancellationToken = default);
        Task<Member?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        #region PlastLevelHistory Methods
        Task AddPlastLevelHistoryAsync(Guid memberKey, PlastLevelHistory history, CancellationToken cancellationToken = default);
        Task UpdatePlastLevelHistoryAsync(Guid memberKey, PlastLevelHistory updatedHistory, CancellationToken cancellationToken = default);
        Task RemovePlastLevelHistoryAsync(Guid memberKey, Guid historyKey, CancellationToken cancellationToken = default);
        Task<IEnumerable<PlastLevelHistory>> GetPlastLevelHistoryAsync(Guid memberKey, CancellationToken cancellationToken = default);
        #endregion

        #region LeadershipHistory Methods

        Task AddLeadershipHistoryAsync(Guid memberKey, LeadershipHistory history, CancellationToken cancellationToken);
        Task EndLeadershipAsync(Guid memberKey, Guid historyKey, DateOnly endDate, CancellationToken cancellationToken);
        Task RemoveLeadershipHistoryAsync(Guid memberKey, Guid historyKey, CancellationToken cancellationToken);
        Task UpdateLeadershipHistoryAsync(Guid memberKey, LeadershipHistory updatedHistory, CancellationToken cancellationToken);

        #endregion
    }
}
