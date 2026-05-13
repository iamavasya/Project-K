namespace ProjectK.API.Helpers;

public sealed class SecurityPatchOptions
{
    // Rollout toggle for upcoming server-side resource authorization filter.
    public bool EnableResourceGuard { get; set; }

    // Geo-blocking settings
    public bool EnableGeoBlocking { get; set; }
    public string[] BlockedCountries { get; set; } = Array.Empty<string>();
}
