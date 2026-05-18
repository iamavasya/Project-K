namespace ProjectK.BusinessLogic.Services.Caching;

public interface IBackendCache
{
    Task<T> GetOrCreateAsync<T>(
        CachePolicy policy,
        string key,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken,
        CacheScopeContext? scopeContext = null);

    void Invalidate(CachePolicy policy);

    void InvalidateByPrefix(string prefix);
}
