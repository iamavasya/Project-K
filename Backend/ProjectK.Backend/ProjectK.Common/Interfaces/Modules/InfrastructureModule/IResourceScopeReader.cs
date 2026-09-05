using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule
{
    /// <summary>
    /// Reads only the keys an authorization decision needs. The entity repositories load a
    /// full graph for editing; ResourceAuthorizeFilter runs on nearly every request, so it
    /// gets its own projection instead.
    /// </summary>
    public interface IResourceScopeReader
    {
        Task<ResourceScope?> GetScopeAsync(
            ResourceType resourceType,
            Guid resourceKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Groups the user leads in this kurin: active гуртковий offices, explicit mentor assignments
        /// (legacy) and the group they are a member of. This is the "own groups" scope tier.
        /// </summary>
        Task<IReadOnlyCollection<Guid>> GetLedGroupKeysAsync(
            Guid userKey,
            Guid kurinKey,
            CancellationToken cancellationToken = default);
    }
}
