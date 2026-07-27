using ProjectK.Common.Entities.AuthModule;

namespace ProjectK.Common.Extensions
{
    public static class AppUserScopeExtensions
    {
        /// <summary>
        /// Kurin the user's tokens are scoped to: the kurin an admin stepped into, otherwise
        /// their own. Every place that mints an access token must go through this, or a refresh
        /// would silently widen an admin back to system-wide access.
        /// </summary>
        public static Guid? ResolveScopeKurinKey(this AppUser user)
        {
            return Normalize(user.ActiveKurinKey) ?? Normalize(user.KurinKey);
        }

        public static string? ResolveScopeKurinKeyString(this AppUser user)
        {
            return user.ResolveScopeKurinKey()?.ToString();
        }

        private static Guid? Normalize(Guid? kurinKey)
        {
            return kurinKey is null || kurinKey == Guid.Empty ? null : kurinKey;
        }
    }
}
