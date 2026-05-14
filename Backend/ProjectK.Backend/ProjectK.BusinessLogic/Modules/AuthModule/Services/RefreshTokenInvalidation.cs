using ProjectK.Common.Entities.AuthModule;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Services
{
    internal static class RefreshTokenInvalidation
    {
        public static void RevokeRefreshToken(AppUser user)
        {
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
        }
    }
}
