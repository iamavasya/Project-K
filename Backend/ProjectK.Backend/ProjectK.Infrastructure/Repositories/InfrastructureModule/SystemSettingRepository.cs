using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Infrastructure.DbContexts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Repositories.InfrastructureModule
{
    public class SystemSettingRepository : ISystemSettingRepository
    {
        private readonly AppDbContext _context;

        public SystemSettingRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Create(SystemSetting entity, CancellationToken cancellationToken = default)
        {
            _context.SystemSettings.Add(entity);
        }

        public void Delete(SystemSetting entity, CancellationToken cancellationToken = default)
        {
            _context.SystemSettings.Remove(entity);
        }

        public Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("SystemSetting uses a string key, not a Guid.");
        }

        public async Task<IEnumerable<SystemSetting>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SystemSettings.ToListAsync(cancellationToken);
        }

        public Task<SystemSetting?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException("SystemSetting uses a string key, not a Guid.");
        }

        public async Task<SystemSetting?> GetByKeyAsync(string key, CancellationToken token = default)
        {
            return await _context.SystemSettings.FirstOrDefaultAsync(x => x.Key == key, token);
        }

        public void Update(SystemSetting entity, CancellationToken cancellationToken = default)
        {
            _context.SystemSettings.Update(entity);
        }
    }
}
