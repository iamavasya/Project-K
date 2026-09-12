namespace ProjectK.Common.Models.Records;

/// <summary>
/// What one user is allowed to see on the agenda. A member sees items aimed at the kurin, at a group
/// they belong to (<see cref="ViewerGroupKeys"/>, which for a mentor also covers assigned groups) or at
/// themselves (<see cref="ViewerMemberKey"/>). Leadership that manages the whole kurin sets
/// <see cref="CanSeeWholeKurin"/> and bypasses the assignment filter.
/// </summary>
public sealed record AgendaViewerScope(
    Guid KurinKey,
    Guid? ViewerMemberKey,
    IReadOnlyCollection<Guid> ViewerGroupKeys,
    IReadOnlyCollection<Guid> ViewerLeadershipKeys,
    bool CanSeeWholeKurin);
