using ProjectK.Common.Models.Records;

namespace ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;

/// <summary>
/// What the probes and вмілості module will tell others about a person. It is the whole of the way
/// in: nobody outside reads <c>ProbeProgress</c> or <c>BadgeProgress</c> for someone else's sake.
/// <para>
/// The member's dossier is composed through this rather than joined to it. A join would read another
/// module's tables in the same query and could never be answered over a network; a call can.
/// </para>
/// </summary>
public interface IMemberProgressDirectory
{
    /// <summary>Everything one person has taken and earned, in one call.</summary>
    Task<MemberProgress> GetForMemberAsync(Guid memberKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// How much there is, without reading it. The dossier index says what a folder holds before
    /// anyone asks to open it.
    /// </summary>
    Task<int> CountForMemberAsync(Guid memberKey, CancellationToken cancellationToken = default);
}
