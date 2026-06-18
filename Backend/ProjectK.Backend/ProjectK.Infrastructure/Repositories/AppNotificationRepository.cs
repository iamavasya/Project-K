using Microsoft.EntityFrameworkCore;
using ProjectK.Common.Entities.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Infrastructure.DbContexts;

namespace ProjectK.Infrastructure.Repositories
{
    public class AppNotificationRepository : IAppNotificationRepository
    {
        private readonly AppDbContext _context;

        public AppNotificationRepository(AppDbContext context)
        {
            _context = context;
        }

        public void Create(AppNotification entity, CancellationToken cancellationToken = default)
        {
            _context.AppNotifications.Add(entity);
        }

        public void Delete(AppNotification entity, CancellationToken cancellationToken = default)
        {
            _context.AppNotifications.Remove(entity);
        }

        public async Task<AppNotification?> GetByKeyAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.AppNotifications
                .AsTracking()
                .FirstOrDefaultAsync(x => x.NotificationKey == entityKey, cancellationToken);
        }

        public async Task<IEnumerable<AppNotification>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.AppNotifications
                .AsNoTracking()
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> ExistsAsync(Guid entityKey, CancellationToken cancellationToken = default)
        {
            return await _context.AppNotifications
                .AnyAsync(x => x.NotificationKey == entityKey, cancellationToken);
        }

        public void Update(AppNotification entity, CancellationToken cancellationToken = default)
        {
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached)
            {
                _context.AppNotifications.Update(entity);
                return;
            }

            entry.State = EntityState.Modified;
        }

        public async Task<IReadOnlyList<AppNotification>> GetInboxAsync(
            Guid recipientUserKey,
            bool unreadOnly,
            int take,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            var query = ActiveForRecipient(recipientUserKey, nowUtc)
                .AsNoTracking();

            if (unreadOnly)
            {
                query = query.Where(x => x.ReadAtUtc == null);
            }

            return await query
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(take)
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetUnreadCountAsync(
            Guid recipientUserKey,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            return await ActiveForRecipient(recipientUserKey, nowUtc)
                .Where(x => x.ReadAtUtc == null)
                .CountAsync(cancellationToken);
        }

        public async Task<AppNotification?> GetByRecipientAndKeyAsync(
            Guid recipientUserKey,
            Guid notificationKey,
            CancellationToken cancellationToken = default)
        {
            return await _context.AppNotifications
                .AsTracking()
                .FirstOrDefaultAsync(
                    x => x.RecipientUserKey == recipientUserKey
                         && x.NotificationKey == notificationKey,
                    cancellationToken);
        }

        public async Task<AppNotification?> GetUnreadByDeduplicationKeyAsync(
            Guid recipientUserKey,
            string deduplicationKey,
            DateTime nowUtc,
            CancellationToken cancellationToken = default)
        {
            return await ActiveForRecipient(recipientUserKey, nowUtc)
                .AsTracking()
                .FirstOrDefaultAsync(
                    x => x.ReadAtUtc == null
                         && x.DeduplicationKey == deduplicationKey,
                    cancellationToken);
        }

        public async Task<int> MarkAllAsReadAsync(
            Guid recipientUserKey,
            DateTime readAtUtc,
            CancellationToken cancellationToken = default)
        {
            return await _context.AppNotifications
                .Where(x => x.RecipientUserKey == recipientUserKey && x.ReadAtUtc == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.ReadAtUtc, readAtUtc)
                    .SetProperty(x => x.UpdatedDate, readAtUtc),
                    cancellationToken);
        }

        private IQueryable<AppNotification> ActiveForRecipient(Guid recipientUserKey, DateTime nowUtc)
        {
            return _context.AppNotifications
                .Where(x => x.RecipientUserKey == recipientUserKey
                            && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > nowUtc));
        }
    }
}
