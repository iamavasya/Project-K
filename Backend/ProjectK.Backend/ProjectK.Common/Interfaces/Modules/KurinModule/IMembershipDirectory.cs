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
}
