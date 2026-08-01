namespace ProjectK.API.Helpers;

public sealed class SecurityPatchOptions
{
    // Rollout toggle for upcoming server-side resource authorization filter.
    public bool EnableResourceGuard { get; set; }

    // Geo-blocking settings
    public bool EnableGeoBlocking { get; set; }
    public string[] BlockedCountries { get; set; } = Array.Empty<string>();

    // Header set by an upstream proxy/CDN (e.g. Cloudflare "CF-IPCountry") carrying the
    // visitor country. When present it is trusted and no outbound GeoIP call is made.
    // Defaults to "CF-IPCountry" when unset.
    public string? GeoCountryHeader { get; set; }
}
