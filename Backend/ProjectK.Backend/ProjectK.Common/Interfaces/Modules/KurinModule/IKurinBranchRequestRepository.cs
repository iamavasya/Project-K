using ProjectK.Common.Entities.KurinModule;

namespace ProjectK.Common.Interfaces.Modules.KurinModule;

public interface IKurinBranchRequestRepository : IBaseEntityRepository<KurinBranchRequest>
{
    /// <summary>The request a kurin is waiting on, if it has one: a kurin keeps at most one open.</summary>
    Task<KurinBranchRequest?> GetPendingForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);

    /// <summary>Every request still waiting for an administrator, oldest first.</summary>
    Task<IReadOnlyList<KurinBranchRequest>> GetPendingAsync(CancellationToken cancellationToken = default);
}
