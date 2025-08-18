using ProjectK.Common.Interfaces.Modules.KurinModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces
{
    public interface IUnitOfWork
    {
        IKurinRepository Kurins { get; }
        IGroupRepository Groups { get; }
        Task<int> SaveChangesAsync(CancellationToken token = default);
    }
}
