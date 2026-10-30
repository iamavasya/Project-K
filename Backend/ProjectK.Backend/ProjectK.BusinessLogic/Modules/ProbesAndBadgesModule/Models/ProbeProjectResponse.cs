namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;

/// <param name="Description">Markdown; pictures are <c>project-image:{key}</c>, their links in <paramref name="Images"/>.</param>
public sealed record ProbeProjectResponse(
    Guid ProjectKey,
    string Title,
    bool IsIndividual,
    string Description,
    string? DirectionMentorName,
    string? DirectionMentorContact,
    DateTime UpdatedAtUtc,
    string? UpdatedByName,
    IReadOnlyList<ProbeProjectImageResponse> Images);

/// <param name="Url">A signed link that expires; never store it, store the key.</param>
public sealed record ProbeProjectImageResponse(Guid ImageKey, string Url);

/// <summary>The limits as numbers, so a client says the same thing the server enforces.</summary>
public sealed record ProbeProjectLimitsResponse(
    int MaxProjects,
    int MaxImagesPerProject,
    long MaxImageBytes,
    int MaxDescriptionLength);

/// <param name="IsProjectProbe">Whether the probe stands at the third place of the person's line.
/// Only that one has projects; for any other the rest is empty.</param>
/// <param name="CanEdit">The person themself, or whoever the kurin lets sign their probe.</param>
public sealed record ProbeProjectsResponse(
    bool IsProjectProbe,
    bool CanEdit,
    IReadOnlyList<ProbeProjectResponse> Projects,
    ProbeProjectLimitsResponse Limits);
