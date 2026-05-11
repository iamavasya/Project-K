using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces.Modules.KurinModule
{
    public interface IMemberWarningRepository : IBaseEntityRepository<MemberWarning>
    {
        Task<IReadOnlyCollection<MemberWarning>> GetByMemberKeyAsync(Guid memberKey, CancellationToken cancellationToken = default);
        Task<IReadOnlyCollection<MemberWarning>> GetActiveByMemberKeyAsync(Guid memberKey, DateTime nowUtc, CancellationToken cancellationToken = default);
        Task<MemberWarning?> GetActiveByMemberAndLevelAsync(Guid memberKey, MemberWarningLevel level, DateTime nowUtc, CancellationToken cancellationToken = default);
        Task<int> ExpireActiveWarningsAsync(DateTime nowUtc, CancellationToken cancellationToken = default);
    }
}
