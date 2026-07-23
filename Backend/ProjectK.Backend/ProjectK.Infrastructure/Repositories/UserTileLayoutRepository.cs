using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.AuthModule;
using ProjectK.Infrastructure.DbContexts;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ProjectK.Infrastructure.Repositories
{
    public class UserTileLayoutRepository : IUserTileLayoutRepository
    {
        private readonly AppDbContext _context;

        public UserTileLayoutRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Create(UserTileLayout entity, CancellationToken cancellationToken = default)
        {
            _context.UserTileLayouts.Add(entity);
        }

        public void Delete(UserTileLayout entity, CancellationToken cancellationToken = default)
        {
            _context.UserTileLayouts.Remove(entity);
        }

        public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.UserTileLayouts
                .AnyAsync(x => x.UserTileLayoutKey == entityKey, cancellationToken);
        }

        public async Task<IEnumerable<UserTileLayout>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.UserTileLayouts.ToListAsync(cancellationToken);
        }

        public async Task<UserTileLayout?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.UserTileLayouts
                .FirstOrDefaultAsync(x => x.UserTileLayoutKey == entityKey, cancellationToken);
        }

        public async Task<IEnumerable<UserTileLayout>> GetByUserAsync(Guid userKey, CancellationToken token = default)
        {
            return await _context.UserTileLayouts
                .Where(x => x.UserKey == userKey)
                .ToListAsync(token);
        }

        public async Task<UserTileLayout?> GetByBoardAsync(Guid userKey, string boardKey, CancellationToken token = default)
        {
            return await _context.UserTileLayouts
                .FirstOrDefaultAsync(x => x.UserKey == userKey && x.BoardKey == boardKey, token);
        }

        public void Update(UserTileLayout entity, CancellationToken cancellationToken = default)
        {
            _context.UserTileLayouts.Update(entity);
        }
    }
}
