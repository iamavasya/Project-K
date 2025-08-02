using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces
{
    public interface IBaseEntityRepository<Entity, Dto>
    {
        Task<Guid> CreateAsync(Dto dto, CancellationToken token = default);
        Task<Entity?> GetByKeyAsync(Guid entityKey, CancellationToken token = default);
        Task<bool> UpdateAsync(Dto dto, CancellationToken token = default);
        Task<bool> DeleteAsync(Guid entityKey, CancellationToken token = default);
        Task<Entity> UpsertAsync(Dto dto, CancellationToken token = default);
        Task<Entity> GetByKeyOrCreateAsync (Dto dto, CancellationToken token = default);
    }
}
