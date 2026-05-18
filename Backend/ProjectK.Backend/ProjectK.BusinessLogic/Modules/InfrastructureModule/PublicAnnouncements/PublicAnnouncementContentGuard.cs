using System.Text.RegularExpressions;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements;

internal static class PublicAnnouncementContentGuard
{
    private static readonly (Regex Pattern, string Reason)[] Patterns =
    [
        (new Regex(@"[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}", RegexOptions.IgnoreCase | RegexOptions.Compiled), "email"),
        (new Regex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled), "ip"),
        (new Regex(@"(?i)bearer\s+[a-z0-9._~+/=-]+", RegexOptions.Compiled), "bearer-token"),
        (new Regex(@"(?i)\b(token|password|secret|apikey|api_key|access_key|refresh_token)\s*[=:]\s*[^\s;&]+", RegexOptions.Compiled), "secret"),
        (new Regex(@"\beyJ[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+\.[a-zA-Z0-9_-]+\b", RegexOptions.Compiled), "jwt"),
        (new Regex(@"^\s*at\s+\S+\(.*\)$", RegexOptions.Multiline | RegexOptions.Compiled), "stack-trace"),
        (new Regex(@"\b[0-9a-f]{16,32}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "trace-id")
    ];

    public static bool ContainsSensitiveData(
        string? title,
        string? body,
        string? imageAltText,
        out string? reason)
    {
        reason = null;
        var content = string.Join("\n", title, body, imageAltText).Trim();
        if (string.IsNullOrWhiteSpace(content))
        {
            return false;
        }

        foreach (var (pattern, label) in Patterns)
        {
            if (pattern.IsMatch(content))
            {
                reason = label;
                return true;
            }
        }

        return false;
    }
}
