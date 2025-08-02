using ProjectK.BusinessLogic.Modules.KurinModule.Commands;
using ProjectK.Common.Dtos;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Interfaces.Modules.KurinModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Repositories
{
    public class KurinRepository : IKurinRepository
    {
        public Task<Guid> CreateAsync(KurinDto dto, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(Guid entityKey, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<Kurin?> GetByKeyAsync(Guid entityKey, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<Kurin> GetByKeyOrCreateAsync(KurinDto dto, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> UpdateAsync(KurinDto dto, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }

        public Task<Kurin> UpsertAsync(KurinDto dto, CancellationToken token = default)
        {
            throw new NotImplementedException();
        }
    }
}
