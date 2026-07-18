namespace ProjectK.BusinessLogic.Services.Caching;

public static class BackendCachePolicies
{
    public static readonly TimeSpan EntityReadTtl = TimeSpan.FromMinutes(1);

    public static readonly CachePolicy KurinReads = new(
        Prefix: "kurin",
        Ttl: EntityReadTtl,
        Scope: CacheScope.Shared);

    public static readonly CachePolicy GroupReads = new(
        Prefix: "group",
        Ttl: EntityReadTtl,
        Scope: CacheScope.Shared);

    public static readonly CachePolicy SystemSettingReads = new(
        Prefix: "system-setting",
        Ttl: TimeSpan.FromMinutes(5),
        Scope: CacheScope.Shared);
}
