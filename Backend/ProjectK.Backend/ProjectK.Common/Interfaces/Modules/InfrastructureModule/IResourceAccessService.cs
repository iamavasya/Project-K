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
}