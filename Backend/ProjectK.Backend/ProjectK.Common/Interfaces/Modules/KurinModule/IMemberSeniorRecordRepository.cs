using ProjectK.Common.Entities.KurinModule;

namespace ProjectK.Common.Interfaces.Modules.KurinModule;

public interface IMemberSeniorRecordRepository : IBaseEntityRepository<MemberSeniorRecord>
{
    /// <summary>A person's книжечка, untracked.</summary>
    Task<IReadOnlyCollection<MemberSeniorRecord>> GetByMemberKeyAsync(Guid memberKey, CancellationToken cancellationToken = default);
}
