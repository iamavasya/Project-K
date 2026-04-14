using ProjectK.ProbeAndBadges.Abstractions;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services
{
    public sealed class BadgesCatalogService : IBadgesCatalogService
    {
        private readonly IBadgesCatalog _badgesCatalog;

        public BadgesCatalogService(IBadgesCatalog badgesCatalog)
        {
            _badgesCatalog = badgesCatalog;
        }

        public BadgesMetadata GetBadgesMetadata()
        {
            return _badgesCatalog.Metadata;
        }

        public IReadOnlyList<Badge> GetBadges(int take)
        {
            var safeTake = Math.Clamp(take, 1, 1000);
            return _badgesCatalog.GetAll().Take(safeTake).ToList();
        }

        public Badge? GetBadgeById(string id)
        {
            return _badgesCatalog.GetById(id);
        }
    }
}
