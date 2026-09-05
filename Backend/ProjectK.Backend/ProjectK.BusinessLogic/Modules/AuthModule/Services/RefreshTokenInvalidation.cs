using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Interfaces.Modules.AuthModule;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Services
{
    /// <summary>
    /// Ends every session an account holds.
    /// <para>
    /// Used by the changes that must not leave an older session alive: a new password, a confirmed
    /// email change, anything touching the second factor. It used to be one line clearing the single
    /// token column; now that an account can be signed in in several places, "revoke" has to mean all
    /// of them, or a change made because of a suspected compromise would leave the intruder logged in.
    /// </para>
    /// </summary>
    internal static class RefreshTokenInvalidation
    {
        public static Task RevokeRefreshTokenAsync(
            IRefreshTokenStore refreshTokens,
            AppUser user,
            CancellationToken cancellationToken = default)
            => refreshTokens.RevokeAllAsync(user.Id, cancellationToken);
    }
}
