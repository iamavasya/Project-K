using MediatR;
using ProjectK.BusinessLogic.Services.Caching;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;

namespace ProjectK.BusinessLogic.Behaviors;

/// <summary>
/// Serves responses for queries marked <see cref="ICacheableQuery"/> from <see cref="IBackendCache"/>.
/// On a cache hit the handler never runs; on a miss the handler runs once and its result is stored.
/// Requests without the marker pass straight through.
/// </summary>
public sealed class CachingBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IBackendCache _cache;
    private readonly ICurrentUserContext _currentUserContext;

    public CachingBehavior(IBackendCache cache, ICurrentUserContext currentUserContext)
    {
        _cache = cache;
        _currentUserContext = currentUserContext;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheable)
        {
            return await next(cancellationToken);
        }

        // Shared entries need no per-caller context; scoped entries key on the current user/roles.
        var scopeContext = cacheable.CachePolicy.Scope == CacheScope.Shared
            ? null
            : CacheScopeContext.From(_currentUserContext);

        return await _cache.GetOrCreateAsync(
            cacheable.CachePolicy,
            cacheable.CacheKey,
            _ => next(cancellationToken),
            cancellationToken,
            scopeContext);
    }
}
