using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Enums;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces.Modules.KurinModule
{
    public interface IMemberAwardRepository : IBaseEntityRepository<MemberAward>
    {
        Task<IReadOnlyCollection<MemberAward>> GetByMemberKeyAsync(Guid memberKey, CancellationToken cancellationToken = default);
        Task<MemberAward?> GetByMemberAndLevelAsync(Guid memberKey, MemberAwardLevel level, CancellationToken cancellationToken = default);
    }
}
