using ProjectK.Common.Entities.KurinModule;

namespace ProjectK.Common.Interfaces.Modules.KurinModule;

public interface IMembershipRequestRepository : IBaseEntityRepository<MembershipRequest>
{
    /// <summary>One request, tracked, or null.</summary>
    Task<MembershipRequest?> GetAsync(Guid membershipRequestKey, CancellationToken cancellationToken = default);

    /// <summary>Every request a kurin sent or received, newest first.</summary>
    Task<IReadOnlyList<MembershipRequest>> GetForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);

    /// <summary>The asking kurin's requests about one person that still wait for an answer.</summary>
    Task<IReadOnlyList<MembershipRequest>> GetPendingAsync(Guid memberKey, Guid toKurinKey, CancellationToken cancellationToken = default);

    /// <summary>The requests about one person that still wait for the answer of the kurin they stand in, tracked.</summary>
    Task<IReadOnlyList<MembershipRequest>> GetPendingFromAsync(Guid memberKey, Guid fromKurinKey, CancellationToken cancellationToken = default);
}
