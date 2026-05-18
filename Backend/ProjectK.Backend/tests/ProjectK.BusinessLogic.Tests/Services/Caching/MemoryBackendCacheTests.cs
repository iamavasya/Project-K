using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using ProjectK.BusinessLogic.Services.Caching;

namespace ProjectK.BusinessLogic.Tests.Services.Caching;

public class MemoryBackendCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_WhenConcurrentMissesUseSameKey_ShouldRunFactoryOnce()
    {
        var cache = CreateCache();
        var policy = new CachePolicy("test", TimeSpan.FromMinutes(1));
        var factoryCalls = 0;

        var tasks = Enumerable.Range(0, 10)
            .Select(_ => cache.GetOrCreateAsync(
                policy,
                "same-key",
                async _ =>
                {
                    Interlocked.Increment(ref factoryCalls);
                    await Task.Delay(25);
                    return "cached-value";
                },
                CancellationToken.None))
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, result => Assert.Equal("cached-value", result));
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task InvalidateByPrefix_WhenMissCompletesAfterInvalidation_ShouldNotExposeStaleValue()
    {
        var cache = CreateCache();
        var policy = new CachePolicy("test", TimeSpan.FromMinutes(1));
        var factoryStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowFactoryToComplete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var staleRequest = cache.GetOrCreateAsync(
            policy,
            "same-key",
            async _ =>
            {
                factoryStarted.SetResult();
                await allowFactoryToComplete.Task;
                return "stale-value";
            },
            CancellationToken.None);

        await factoryStarted.Task;
        cache.InvalidateByPrefix("test");
        allowFactoryToComplete.SetResult();

        Assert.Equal("stale-value", await staleRequest);

        var freshValue = await cache.GetOrCreateAsync(
            policy,
            "same-key",
            _ => Task.FromResult("fresh-value"),
            CancellationToken.None);

        Assert.Equal("fresh-value", freshValue);
    }

    private static MemoryBackendCache CreateCache()
    {
        return new MemoryBackendCache(
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<MemoryBackendCache>.Instance);
    }
}
