using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.Common.Interfaces.Modules.KurinModule;

/// <summary>
/// The kurin's past: every office holding, the memberships that ended and the гуртки dissolved.
/// It is the one reader that sees past the dissolved-гурток filter. The <c>Find…</c> methods load
/// tracked, for a correction or a deletion; the <c>Get…</c> ones read.
/// </summary>
public interface IKurinArchiveRepository
{
    Task<IReadOnlyList<ArchivedOffice>> GetOfficesAsync(Guid kurinKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ArchivedMembership>> GetFormerMembershipsAsync(Guid kurinKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DissolvedGroup>> GetDissolvedGroupsAsync(Guid kurinKey, CancellationToken cancellationToken = default);

    /// <summary>The holding with its office and the office's гурток, dissolved or not.</summary>
    Task<LeadershipHistory?> FindOfficeAsync(Guid historyKey, CancellationToken cancellationToken = default);

    Task<Membership?> FindMembershipAsync(Guid membershipKey, CancellationToken cancellationToken = default);

    /// <summary>Every гурток the membership was ever in, its current one included.</summary>
    Task<IReadOnlyList<Guid>> GetStintGroupKeysAsync(Guid membershipKey, CancellationToken cancellationToken = default);

    Task<Group?> FindDissolvedGroupAsync(Guid groupKey, CancellationToken cancellationToken = default);

    void RemoveOffice(LeadershipHistory history);

    /// <summary>
    /// Marks for deletion the person's holdings in this kurin that overlap the period, and reports
    /// them. A membership deleted «as if they had never been here» takes its offices with it.
    /// </summary>
    Task<IReadOnlyList<LeadershipHistory>> RemoveOfficesHeldAsync(
        Guid memberKey,
        Guid kurinKey,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    /// <summary>Marks the membership for deletion; its гурток stints go with it in the database.</summary>
    void RemoveMembership(Membership membership);

    /// <summary>
    /// Marks a dissolved гурток for deletion with its провід, that провід's history and the stints
    /// spent in it. Reports how many holdings went.
    /// </summary>
    Task<int> RemoveDissolvedGroupAsync(Group group, CancellationToken cancellationToken = default);
}
