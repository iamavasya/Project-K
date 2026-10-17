namespace ProjectK.Common.Models.Settings;

/// <summary>
/// The anonymous daily ping to the status service: a random instance id, the release and a few
/// counts, nothing that names a person or a kurin. On in Production and SelfHost; a self-host turns
/// it off with <c>PROJECTK_TELEMETRY=false</c>.
/// </summary>
public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public bool Enabled { get; set; }

    public string Endpoint { get; set; } = "https://status.rostyslav-mukha.dev/v1/ping";

    /// <summary>The first ping waits this long after startup, so a crash loop does not ping.</summary>
    public int InitialDelayMinutes { get; set; } = 5;

    public int IntervalHours { get; set; } = 24;

    public int TimeoutSeconds { get; set; } = 15;
}
