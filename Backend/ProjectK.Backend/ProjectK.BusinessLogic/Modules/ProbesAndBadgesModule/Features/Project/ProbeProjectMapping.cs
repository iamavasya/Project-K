using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Project;

internal static class ProbeProjectMapping
{
    public static readonly ProbeProjectLimitsResponse Limits = new(
        ProbeProjectRules.MaxProjects,
        ProbeProjectRules.MaxImagesPerProject,
        ProbeProjectRules.MaxImageBytes,
        ProbeProjectRules.MaxDescriptionLength);

    public static ProbeProjectResponse ToResponse(ProbeProject project, IBlobReadLinks links) => new(
        project.ProbeProjectKey,
        project.Title,
        project.IsIndividual,
        project.Description,
        project.DirectionMentorName,
        project.DirectionMentorContact,
        project.UpdatedDate,
        project.UpdatedByName,
        project.Images
            .OrderBy(image => image.CreatedDate)
            .Select(image => ToResponse(image, links))
            .ToList());

    public static ProbeProjectImageResponse ToResponse(ProbeProjectImage image, IBlobReadLinks links) =>
        new(image.ProbeProjectImageKey, links.For(image.BlobName)!);
}

/// <summary>What every write to a project checks first: the right probe, and the right person.</summary>
internal static class ProbeProjectGuard
{
    public static async Task<ServiceResult<T>?> RefuseAsync<T>(
        IProbeProjectAccess access,
        Guid memberKey,
        string probeId,
        CancellationToken cancellationToken)
    {
        if (!await access.IsProjectProbeAsync(memberKey, probeId, cancellationToken))
        {
            return ServiceResult<T>.Failure(
                ResultType.BadRequest, "NotProjectProbe", "Only the third probe of the person's line has projects.");
        }

        if (!await access.CanEditAsync(memberKey, cancellationToken))
        {
            return new ServiceResult<T>(ResultType.Forbidden);
        }

        return null;
    }
}
