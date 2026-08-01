using ProjectK.BusinessLogic.Services.Caching;

namespace ProjectK.BusinessLogic.Behaviors;

/// <summary>
/// Opt-in marker for read queries whose response may be cached by
/// <see cref="CachingBehavior{TRequest,TResponse}"/>. Only implement it on queries whose
/// result is safe to serve from cache for the whole <see cref="Services.Caching.CachePolicy.Ttl"/>.
/// </summary>
public interface ICacheableQuery
{
    /// <summary>Prefix, TTL and scope (Shared / per-user / per-permission-context) for this query.</summary>
    CachePolicy CachePolicy { get; }

    /// <summary>Key unique to this query's arguments within <see cref="CachePolicy"/>'s prefix and scope.</summary>
    string CacheKey { get; }
}
