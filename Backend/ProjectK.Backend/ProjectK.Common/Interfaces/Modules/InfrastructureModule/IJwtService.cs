using ProjectK.Common.Models.Dtos.AuthModule;

namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule
{
    public interface IJwtService
    {
        string GenerateAccessToken(string userId, string email, IEnumerable<string> roles, string? kurinKey);

        RefreshToken GenerateRefreshToken();

        /// <summary>
        /// A short-lived proof that the password step passed for this account. The second-factor
        /// step requires it, so a code on its own never completes a sign-in.
        /// </summary>
        string GenerateMfaChallengeToken(Guid userId);

        /// <summary>
        /// The account a challenge was minted for, or <c>null</c> when the token is invalid, expired,
        /// or was issued for another purpose — an access token is not a challenge.
        /// </summary>
        Guid? ReadMfaChallenge(string token);
    }
}
