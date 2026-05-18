namespace ProjectK.BusinessLogic.Services.Caching;

public sealed record CachePolicy(
    string Prefix,
    TimeSpan Ttl,
    CacheScope Scope = CacheScope.Shared);
