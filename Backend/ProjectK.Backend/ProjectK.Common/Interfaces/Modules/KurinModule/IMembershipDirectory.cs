using ProjectK.Common.Models.Records;

namespace ProjectK.Common.Interfaces.Modules.KurinModule;

/// <summary>
/// What the kurin module will tell others about a person's belonging. The counterpart of
/// <c>IMemberDirectory</c>: that one answers "who is this person", this one answers "where have they
/// been", and neither module reads the other's tables to do it.
/// </summary>
public interface IMembershipDirectory
{
    /// <summary>
    /// Every kurin this person has belonged to, current first and then by when they joined. Past
    /// memberships are included: leaving a kurin does not remove it from a person's history.
    /// </summary>
    Task<IReadOnlyCollection<MembershipRecord>> GetForMemberAsync(
        Guid memberKey,
        CancellationToken cancellationToken = default);

    /// <summary>How many, without reading them.</summary>
    Task<int> CountForMemberAsync(Guid memberKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// The kurins an account may currently act in. Asked by the access layer, so it is answered from
    /// the account key that memberships carry and never by way of the person behind it.
    /// </summary>
    Task<IReadOnlyCollection<Guid>> GetKurinKeysForAccountAsync(
        Guid userKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The same, named — what an account is shown when it has to choose which kurin to act in.
    /// </summary>
    Task<IReadOnlyCollection<MembershipRecord>> GetCurrentForAccountAsync(
        Guid userKey,
        CancellationToken cancellationToken = default);
}
