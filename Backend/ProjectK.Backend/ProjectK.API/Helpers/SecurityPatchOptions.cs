namespace ProjectK.API.Helpers;

public sealed class SecurityPatchOptions
{
    // Rollout toggle for upcoming server-side resource authorization filter.
    public bool EnableResourceGuard { get; set; }

    // Geo-blocking settings
    public bool EnableGeoBlocking { get; set; }
    public string[] BlockedCountries { get; set; } = Array.Empty<string>();

    // Tailscale protection
    public bool EnableTailscaleOnlyForAdmins { get; set; }
    public string TailscaleIpRange { get; set; } = "100.64.0.0/10"; // Default Tailscale range
}
