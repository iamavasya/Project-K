namespace ProjectK.BusinessLogic.Tests.TestHelpers;

/// <summary>
/// A clock stopped at a chosen instant, for the rules that depend on "now" — invitation and token
/// expiry, warning windows. Before <see cref="TimeProvider"/> was injected those branches could only
/// be reached by constructing data relative to the real clock.
/// </summary>
public sealed class FixedTimeProvider : TimeProvider
{
    private DateTimeOffset _utcNow;

    public FixedTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

    public override DateTimeOffset GetUtcNow() => _utcNow;

    /// <summary>Moves the clock forward, for tests that need something to fall out of range.</summary>
    public void Advance(TimeSpan by) => _utcNow = _utcNow.Add(by);
}
