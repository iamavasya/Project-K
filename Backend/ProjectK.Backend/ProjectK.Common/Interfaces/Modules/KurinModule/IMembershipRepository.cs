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

        /// <summary>
        /// Puts a person in a kurin and гурток, opening the membership if it is not open yet.
        /// <para>
        /// One kurin at a time, deliberately: a member record still names a single kurin, so a
        /// placement that names a different one is a move, and every other current membership is
        /// closed. When joining and leaving become use cases of their own, this collapses into
        /// "open a membership" and the closing goes with it.
        /// </para>
        /// The write is left on the unit of work, so it commits with whatever caused it.
        /// </summary>
        Task PlaceAsync(
            Guid memberKey,
            Guid? userKey,
            Guid kurinKey,
            Guid? groupKey,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Drops every membership these people held, current or past. Called when the people
        /// themselves are gone: nothing in the database ties the two tables together, so nobody else
        /// would clear these rows.
        /// </summary>
        Task RemoveForMembersAsync(
            IReadOnlyCollection<Guid> memberKeys,
            CancellationToken cancellationToken = default);

        /// <summary>The same for a kurin being deleted — its whole membership history goes with it.</summary>
        Task RemoveForKurinAsync(Guid kurinKey, CancellationToken cancellationToken = default);

        /// <summary>
        /// Forgets a гурток that is being deleted without touching the membership itself: the person
        /// stays in the kurin, just no longer in that гурток.
        /// </summary>
        Task DetachFromGroupAsync(Guid groupKey, CancellationToken cancellationToken = default);
    }
}
