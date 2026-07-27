using ProjectK.Common.Entities.AuthModule;

namespace ProjectK.Common.Extensions
{
    public static class AppUserScopeExtensions
    {
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
