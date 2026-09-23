using ProjectK.Infrastructure.BackgroundServices;

namespace ProjectK.Infrastructure.Tests.Services;

public class DemoResetBackgroundServiceTests
{
    // 03:00 today when it is still ahead, 03:00 tomorrow once it has passed; never "now".
    [Theory]
    [InlineData("2026-09-12T01:30:00Z", "2026-09-12T03:00:00Z")]
    [InlineData("2026-09-12T03:00:00Z", "2026-09-13T03:00:00Z")]
    [InlineData("2026-09-12T22:15:00Z", "2026-09-13T03:00:00Z")]
    public void NextResetAfter_ShouldAlwaysBeInTheFuture_AtTheResetHour(string now, string expected)
    {
        var next = DemoResetBackgroundService.NextResetAfter(DateTime.Parse(now).ToUniversalTime(), 3);

        Assert.Equal(DateTime.Parse(expected).ToUniversalTime(), next);
        Assert.Equal(DateTimeKind.Utc, next.Kind);
    }
}
