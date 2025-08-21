using ProjectK.Common.Entities.KurinModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces.Modules.KurinModule
{
    public interface IGroupRepository : IBaseEntityRepository<Group>
    {
        Task<IEnumerable<Group>> GetAllAsync(Guid kurinKey, CancellationToken token = default);
    }
}
