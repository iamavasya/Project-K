using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule;

public interface IResourceAccessService
{
    Task<ResourceAccessDecision> CheckAccessAsync(
        ResourceType resourceType,
        ResourceAction action,
        Guid resourceKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks <paramref name="resourceType"/> permission but reads the scope from a different entity.
    /// <para>
    /// Signing a probe point is a <c>ProbeProgress</c> permission, yet the route carries the member's
    /// key — the progress record may not exist yet. Without this the endpoints all borrowed
    /// <c>Member:Update</c>, which is how one permission came to cover thirteen operations.
    /// </para>
    /// </summary>
    Task<ResourceAccessDecision> CheckAccessAsync(
        ResourceType resourceType,
        ResourceAction action,
        ResourceType scopeResourceType,
        Guid scopeResourceKey,
        CancellationToken cancellationToken = default);
}