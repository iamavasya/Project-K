namespace ProjectK.Common.Models.Authorization;

/// <summary>
/// What an access token says beyond who the caller is and which roles they hold. Each value is
/// written when the token is minted and read without a query for as long as the token lives.
/// </summary>
public static class AccessTokenClaims
{
    /// <summary>The kurin the token was minted for; absent when the account stands in none.</summary>
    public const string KurinKey = "kurinKey";

    /// <summary>
    /// RFC 8176 authentication methods: <see cref="Mfa"/> when the account has a second factor,
    /// <see cref="Password"/> when it signs in on a password alone. The privileged-MFA gate reads
    /// this instead of asking the account on every mutation. Ten minutes stale at most: enabling
    /// MFA hands the browser a fresh session, and disabling it ends every session.
    /// </summary>
    public const string AuthenticationMethods = "amr";

    /// <summary>
    /// What the bearer handler renames <c>amr</c> to on the way in (its inbound claim map), which is
    /// not <c>ClaimTypes.AuthenticationMethod</c>. 1.1.2 looked for the latter and refused every
    /// privileged save on production.
    /// </summary>
    public const string AuthenticationMethodsMapped = "http://schemas.microsoft.com/claims/authnmethodsreferences";

    public const string Mfa = "mfa";
    public const string Password = "pwd";
}
