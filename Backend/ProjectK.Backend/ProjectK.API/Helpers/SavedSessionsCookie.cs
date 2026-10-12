using Microsoft.AspNetCore.Http;
using ProjectK.Common.Models.Records;

namespace ProjectK.API.Helpers;

/// <summary>
/// The sessions this browser keeps for the accounts it is signed in to but not using. Scoped like
/// the refresh cookie and just as unreadable to scripts: each value in it is a refresh token.
/// </summary>
public static class SavedSessionsCookie
{
    public const string Name = "savedSessions";
    private const char Separator = ',';
    private const string Path = "/api/auth";

    public static void Write(HttpContext context, IReadOnlyCollection<SavedSession> sessions)
    {
        if (sessions.Count == 0)
        {
            Delete(context);
            return;
        }

        var isSecureRequest = IsSecureRequest(context.Request);
        context.Response.Cookies.Append(Name, string.Join(Separator, sessions.Select(session => session.Token)), new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecureRequest,
            SameSite = isSecureRequest ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = sessions.Max(session => session.ExpiresAtUtc),
            Path = Path
        });
    }

    public static void Delete(HttpContext context)
    {
        var isSecureRequest = IsSecureRequest(context.Request);
        context.Response.Cookies.Delete(Name, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecureRequest,
            SameSite = isSecureRequest ? SameSiteMode.None : SameSiteMode.Lax,
            Path = Path
        });
    }

    /// <summary>The kept tokens, oldest first.</summary>
    public static List<string> Read(HttpRequest request)
    {
        return request.Cookies.TryGetValue(Name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value.Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            : [];
    }

    private static bool IsSecureRequest(HttpRequest request)
    {
        return request.IsHttps
            || string.Equals(request.Headers["X-Forwarded-Proto"].FirstOrDefault(), "https", StringComparison.OrdinalIgnoreCase);
    }
}
