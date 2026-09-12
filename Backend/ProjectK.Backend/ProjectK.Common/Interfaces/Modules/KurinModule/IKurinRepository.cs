using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectK.Common.Entities.KurinModule;

namespace ProjectK.Common.Interfaces.Modules.KurinModule;

public interface IKurinRepository : IBaseEntityRepository<Kurin>
{
    Task<Kurin?> GetByNumberAsync(int number, CancellationToken token = default);
    Task<bool> ExistsAsync(int number, CancellationToken token = default);
}
