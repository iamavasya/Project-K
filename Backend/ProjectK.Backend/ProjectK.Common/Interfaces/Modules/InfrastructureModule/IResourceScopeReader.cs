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

        /// <summary>Groups the user mentors in this kurin: explicit assignments plus their own.</summary>
        Task<IReadOnlyCollection<Guid>> GetMentorGroupKeysAsync(
            Guid userKey,
            Guid kurinKey,
            CancellationToken cancellationToken = default);
    }
}
