using ProjectK.Common.Entities.KurinModule;

namespace ProjectK.Common.Interfaces.Modules.KurinModule
{
    public interface IMembershipRepository : IBaseEntityRepository<Membership>
    {
        /// <summary>The kurins a person currently belongs to, newest first.</summary>
        Task<IReadOnlyCollection<Membership>> GetActiveForMemberAsync(
            Guid memberKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Writes the account key onto every current membership of a person. The copy exists so that
        /// authorization never has to read the member record; keeping it correct is this method's job,
        /// and it runs whenever an account is linked.
        /// </summary>
        Task SyncAccountAsync(Guid memberKey, Guid? userKey, CancellationToken cancellationToken = default);
    }
}
