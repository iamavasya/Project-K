using System.Security.Claims;

namespace ProjectK.Common.Extensions;

/// <summary>
/// One definition of how the caller's identity is read out of a token.
/// <para>
/// Four spellings of this used to coexist: controllers read <c>NameIdentifier</c> alone, the MFA
/// middleware fell back to <c>JwtRegisteredClaimNames.Sub</c>, and the activity logger fell back to
/// the literal <c>"sub"</c>. A token carrying only <c>sub</c> therefore passed the middleware and was
/// then rejected by the controller behind it.
/// </para>
/// </summary>
public static class ClaimsPrincipalExtension
{
    /// <summary>The caller's user key, or <c>null</c> when no readable identity claim is present.</summary>
    public static Guid? GetUserKey(this ClaimsPrincipal? principal)
        => Guid.TryParse(principal.GetUserKeyValue(), out var userKey) ? userKey : null;

    /// <summary>
    /// The raw identity claim value, for callers that want the string rather than a parsed key —
    /// a rate-limiter partition, for instance.
    /// </summary>
    public static string? GetUserKeyValue(this ClaimsPrincipal? principal)
        => principal?.FindFirstValue(ClaimTypes.NameIdentifier)
           ?? principal?.FindFirstValue("sub");
}
