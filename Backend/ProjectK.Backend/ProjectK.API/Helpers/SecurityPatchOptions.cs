namespace ProjectK.API.Helpers;

public sealed class SecurityPatchOptions
{
    // Rollout toggle for upcoming server-side resource authorization filter.
    public bool EnableResourceGuard { get; set; }
}
