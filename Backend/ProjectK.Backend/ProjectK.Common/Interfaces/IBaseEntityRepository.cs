using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces
{
    public interface IBaseEntityRepository<T>
    {
        Task<Guid> CreateAsync(T entity);
        Task<T?> GetByKeyAsync(Guid entityKey);
        Task<bool> UpdateAsync(T entity);
        Task<bool> DeleteAsync(Guid entityKey);
        Task<Guid> UpsertAsync(T entity);
        Task<T> GetByKeyOrCreateAsync (Guid entityKey, T entity);
    }
}
