using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Entities.KurinModule;

namespace ProjectK.Common.Interfaces.Modules.KurinModule;

/// <summary>
/// Everything of the kurin's own the report is assembled from, read in one pass. What people have
/// earned is not here: that belongs to the probes and вмілості module and is asked of it through
/// <c>IMemberProgressDirectory</c>.
/// </summary>
public sealed record KurinReportSourceData(
    Kurin Kurin,
    IReadOnlyList<Group> Groups,
    IReadOnlyList<MentorAssignment> MentorAssignments,
    IReadOnlyList<Member> Members,
    IReadOnlyDictionary<Guid, AppUser> UsersByKey,
    IReadOnlyDictionary<Guid, Membership> MembershipByMemberKey);

/// <summary>
/// The report's read side. Splitting it out is what let the report itself move out of the API
/// project: the queries are eager-loaded and report-shaped, so they belong with the database, while
/// the assembly needs the probe and badge catalogues, which live in the business layer.
/// </summary>
public interface IKurinReportSource
{
    /// <summary>
    /// Loads the kurin and everyone in it, or <c>null</c> when the kurin does not exist.
    /// <paramref name="currentUserKey"/> is included in the account lookup so the report can name
    /// whoever asked for it.
    /// </summary>
    Task<KurinReportSourceData?> LoadAsync(
        Guid kurinKey,
        Guid? currentUserKey,
        CancellationToken cancellationToken = default);
}
