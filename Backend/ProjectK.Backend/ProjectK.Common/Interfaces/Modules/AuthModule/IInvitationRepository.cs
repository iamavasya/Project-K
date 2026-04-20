using ProjectK.Common.Entities.AuthModule;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces.Modules.AuthModule
{
    public interface IInvitationRepository : IBaseEntityRepository<Invitation>
    {
        Task<Invitation?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
        Task<Invitation?> GetActiveByWaitlistEntryKeyAsync(Guid waitlistEntryKey, CancellationToken cancellationToken = default);
    }
}
