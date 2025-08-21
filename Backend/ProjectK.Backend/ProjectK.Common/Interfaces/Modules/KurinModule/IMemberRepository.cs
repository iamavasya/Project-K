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
        Task<IEnumerable<Member>> GetAllAsync(Guid groupKey, CancellationToken token = default);
        Task<IEnumerable<Member>> GetAllByKurinKeyAsync(Guid kurinKey, CancellationToken token = default);
    }
}
