using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces
{
    public interface IBaseEntityRepository<T>
    {
        Task<Guid> CreateAsync(T entity, CancellationToken token = default);
        Task<T?> GetByKeyAsync(Guid entityKey, CancellationToken token = default);
        Task<bool> UpdateAsync(T entity, CancellationToken token = default);
        Task<bool> DeleteAsync(Guid entityKey, CancellationToken token = default);
        Task<Guid> UpsertAsync(T entity, CancellationToken token = default);
        Task<T> GetByKeyOrCreateAsync (Guid entityKey, T entity, CancellationToken token = default);
    }
}
