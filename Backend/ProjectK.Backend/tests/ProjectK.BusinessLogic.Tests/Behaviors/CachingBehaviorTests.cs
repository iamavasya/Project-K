using FluentAssertions;
using MediatR;
using Moq;
using ProjectK.BusinessLogic.Behaviors;
using ProjectK.BusinessLogic.Services.Caching;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.Behaviors;

public class CachingBehaviorTests
{
    public sealed record PlainQuery : IRequest<ServiceResult<bool>>;

    public sealed record CacheableQuery(CachePolicy CachePolicy, string CacheKey)
        : IRequest<ServiceResult<bool>>, ICacheableQuery;

    private static readonly CachePolicy SharedPolicy =
        new("test", TimeSpan.FromMinutes(1), CacheScope.Shared);

    private readonly Mock<IBackendCache> _cache = new();
    private readonly Mock<ICurrentUserContext> _currentUser = new();

    [Fact]
    public async Task Handle_SkipsCache_WhenQueryIsNotCacheable()
    {
        var behavior = new CachingBehavior<PlainQuery, ServiceResult<bool>>(_cache.Object, _currentUser.Object);
        var expected = new ServiceResult<bool>(ResultType.Success, true);

        var result = await behavior.Handle(new PlainQuery(), _ => Task.FromResult(expected), CancellationToken.None);

        result.Should().BeSameAs(expected);
        _cache.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_ReturnsCachedValue_WithoutRunningHandler_OnHit()
    {
        var cached = new ServiceResult<bool>(ResultType.Success, true);
        _cache
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<CachePolicy>(),
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<ServiceResult<bool>>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<CacheScopeContext?>()))
            .ReturnsAsync(cached);

        var behavior = new CachingBehavior<CacheableQuery, ServiceResult<bool>>(_cache.Object, _currentUser.Object);
        var handlerRan = false;

        var result = await behavior.Handle(
            new CacheableQuery(SharedPolicy, "key"),
            _ => { handlerRan = true; return Task.FromResult(new ServiceResult<bool>(ResultType.Success, false)); },
            CancellationToken.None);

        result.Should().BeSameAs(cached);
        handlerRan.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_RunsHandlerThroughFactory_OnMiss()
    {
        _cache
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<CachePolicy>(),
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<ServiceResult<bool>>>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<CacheScopeContext?>()))
            .Returns((CachePolicy _, string _, Func<CancellationToken, Task<ServiceResult<bool>>> factory, CancellationToken ct, CacheScopeContext? _) => factory(ct));

        var behavior = new CachingBehavior<CacheableQuery, ServiceResult<bool>>(_cache.Object, _currentUser.Object);
        var fresh = new ServiceResult<bool>(ResultType.Success, true);

        var result = await behavior.Handle(
            new CacheableQuery(SharedPolicy, "key"),
            _ => Task.FromResult(fresh),
            CancellationToken.None);

        result.Should().BeSameAs(fresh);
    }
}
