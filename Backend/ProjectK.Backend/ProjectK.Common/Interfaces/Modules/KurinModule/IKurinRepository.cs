using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProjectK.Common.Entities.KurinModule;

namespace ProjectK.Common.Interfaces.Modules.KurinModule;

public interface IKurinRepository : IBaseEntityRepository<Kurin>
{
    /// <summary>A kurin number is unique within its край only, so a lookup by number names the край.</summary>
    Task<Kurin?> GetByNumberAsync(string kraiCode, int number, CancellationToken token = default);
    Task<bool> ExistsAsync(string kraiCode, int number, CancellationToken token = default);
}
