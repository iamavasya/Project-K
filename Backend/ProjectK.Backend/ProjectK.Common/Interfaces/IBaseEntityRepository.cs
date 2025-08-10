using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Common.Interfaces
{
    public interface IBaseEntityRepository<T>
    {
        void Create(T entity, CancellationToken token = default);
        Task<T?> GetByKeyAsync(Guid entityKey, CancellationToken token = default);
        Task<IEnumerable<T>> GetAllAsync(CancellationToken token = default);
        Task<bool> ExistsAsync(Guid entityKey, CancellationToken token = default);
        void Update(T entity, CancellationToken token = default);
        void Delete(T entity, CancellationToken token = default);

    }
}
