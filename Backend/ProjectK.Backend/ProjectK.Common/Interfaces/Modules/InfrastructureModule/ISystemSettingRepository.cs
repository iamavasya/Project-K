using ProjectK.Common.Entities.InfrastructureModule;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule
{
    public interface ISystemSettingRepository : IBaseEntityRepository<SystemSetting>
    {
        Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken token = default);
    }
}
