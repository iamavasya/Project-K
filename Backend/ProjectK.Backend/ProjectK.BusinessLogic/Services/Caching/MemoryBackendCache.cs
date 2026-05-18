using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace ProjectK.BusinessLogic.Services.Caching;

public sealed class MemoryBackendCache : IBackendCache
{
    private const string KeyVersion = "v1";

    private readonly IMemoryCache _cache;
    private readonly ILogger<MemoryBackendCache> _logger;
    private readonly ConcurrentDictionary<string, byte> _knownKeys = new();
    private readonly ConcurrentDictionary<string, long> _prefixGenerations = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _keyLocks = new();

    public MemoryBackendCache(IMemoryCache cache, ILogger<MemoryBackendCache> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task<T> GetOrCreateAsync<T>(
        CachePolicy policy,
        string key,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken,
        CacheScopeContext? scopeContext = null)
    {
        var cacheKey = BuildCacheKey(policy, key, scopeContext);

        if (_cache.TryGetValue(cacheKey, out T? cachedValue))
        {
            _logger.LogDebug(
                "Cache Hit: PolicyPrefix={PolicyPrefix}, Scope={Scope}, NormalizedKey={NormalizedKey}",
                policy.Prefix, policy.Scope, cacheKey);
            return cachedValue!;
        }

        var keyLock = _keyLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await keyLock.WaitAsync(cancellationToken);
        try
        {
            if (_cache.TryGetValue(cacheKey, out cachedValue))
            {
                _logger.LogDebug(
                    "Cache Hit: PolicyPrefix={PolicyPrefix}, Scope={Scope}, NormalizedKey={NormalizedKey}",
                    policy.Prefix, policy.Scope, cacheKey);
                return cachedValue!;
            }

            _logger.LogDebug(
                "Cache Miss: PolicyPrefix={PolicyPrefix}, Scope={Scope}, NormalizedKey={NormalizedKey}",
                policy.Prefix, policy.Scope, cacheKey);

            var value = await factory(cancellationToken);
            _cache.Set(
                cacheKey,
                value,
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = policy.Ttl,
                    Size = 1
                }.RegisterPostEvictionCallback(static (evictedKey, _, _, state) =>
                {
                    if (evictedKey is string cacheKeyValue && state is MemoryBackendCache cache)
                    {
                        cache.ForgetKey(cacheKeyValue);
                    }
                }, this));

            _knownKeys.TryAdd(cacheKey, 0);

            return value;
        }
        finally
        {
            keyLock.Release();
        }
    }

    public void Invalidate(CachePolicy policy)
    {
        InvalidateByPrefix(policy.Prefix);
    }

    public void InvalidateByPrefix(string prefix)
    {
        var normalizedPrefix = NormalizePart(prefix);
        _prefixGenerations.AddOrUpdate(normalizedPrefix, 1, (_, current) => current + 1);
        var cachePrefix = $"{KeyVersion}:{normalizedPrefix}:";
        var invalidatedCount = 0;

        foreach (var key in _knownKeys.Keys.Where(key => key.StartsWith(cachePrefix, StringComparison.Ordinal)))
        {
            _cache.Remove(key);
            ForgetKey(key);
            invalidatedCount++;
        }

        _logger.LogDebug(
            "Cache Invalidate: Prefix={Prefix}, NormalizedPrefix={NormalizedPrefix}, Generation={Generation}, InvalidatedCount={InvalidatedCount}",
            prefix, normalizedPrefix, _prefixGenerations[normalizedPrefix], invalidatedCount);
    }

    private string BuildCacheKey(CachePolicy policy, string key, CacheScopeContext? scopeContext)
    {
        var normalizedPrefix = NormalizePart(policy.Prefix);
        var generation = _prefixGenerations.GetOrAdd(normalizedPrefix, 0);

        return string.Join(
            ':',
            KeyVersion,
            normalizedPrefix,
            $"g{generation}",
            ResolveScopeKey(policy, scopeContext),
            NormalizePart(key));
    }

    private void ForgetKey(string cacheKey)
    {
        _knownKeys.TryRemove(cacheKey, out _);
        _keyLocks.TryRemove(cacheKey, out _);
    }

    private static string ResolveScopeKey(CachePolicy policy, CacheScopeContext? scopeContext)
    {
        return policy.Scope switch
        {
            CacheScope.Shared => "shared",
            CacheScope.User => $"user:{RequireUserId(scopeContext)}",
            CacheScope.PermissionContext => $"permission:{ResolvePermissionContext(scopeContext)}",
            CacheScope.UserPermissionContext => $"user-permission:{RequireUserId(scopeContext)}:{ResolvePermissionContext(scopeContext)}",
            _ => throw new ArgumentOutOfRangeException(nameof(policy), $"Unsupported cache scope: {policy.Scope}")
        };
    }

    private static Guid RequireUserId(CacheScopeContext? scopeContext)
    {
        return scopeContext?.UserId
            ?? throw new InvalidOperationException("User-scoped cache requires a user id.");
    }

    private static string ResolvePermissionContext(CacheScopeContext? scopeContext)
    {
        if (scopeContext is null)
        {
            throw new InvalidOperationException("Permission-scoped cache requires a scope context.");
        }

        var roles = scopeContext.Roles.Count == 0
            ? "no-role"
            : string.Join(',', scopeContext.Roles.Order(StringComparer.OrdinalIgnoreCase));

        return $"kurin:{scopeContext.KurinKey?.ToString() ?? "none"}:roles:{roles}";
    }

    private static string NormalizePart(string value)
    {
        return value
            .Trim()
            .Replace(' ', '-')
            .Replace(':', '_')
            .ToLowerInvariant();
    }
}
