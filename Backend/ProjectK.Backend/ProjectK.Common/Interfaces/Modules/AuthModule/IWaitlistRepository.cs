using ProjectK.Common.Entities.AuthModule;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces.Modules.AuthModule
{
    public interface IWaitlistRepository : IBaseEntityRepository<WaitlistEntry>
    {
        Task<WaitlistEntry?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    }
}
