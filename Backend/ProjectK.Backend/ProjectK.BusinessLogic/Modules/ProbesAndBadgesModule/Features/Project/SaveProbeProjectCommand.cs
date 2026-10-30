using MediatR;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.BusinessLogic.Services.Audit;
using ProjectK.Common.Entities.ProbesAndBadgesModule;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Audit;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Project;

/// <summary>
/// Adds a project to the third probe (<see cref="ProjectKey"/> null) or rewrites one. Pictures the
/// new description no longer shows are dropped with their blobs.
/// </summary>
public sealed record SaveProbeProjectCommand(
    Guid MemberKey,
    string ProbeId,
    Guid? ProjectKey,
    string? Title,
    bool IsIndividual,
    string? Description,
    string? DirectionMentorName,
    string? DirectionMentorContact) : IRequest<ServiceResult<ProbeProjectResponse>>;

public sealed class SaveProbeProjectCommandHandler : IRequestHandler<SaveProbeProjectCommand, ServiceResult<ProbeProjectResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProbeProjectAccess _access;
    private readonly IBlobReadLinks _links;
    private readonly IPhotoService _photos;
    private readonly AuditActorResolver _actors;
    private readonly IKurinAudit _audit;
    private readonly IProbesCatalogService _probes;

    public SaveProbeProjectCommandHandler(
        IUnitOfWork unitOfWork,
        IProbeProjectAccess access,
        IBlobReadLinks links,
        IPhotoService photos,
        AuditActorResolver actors,
        IKurinAudit audit,
        IProbesCatalogService probes)
    {
        _unitOfWork = unitOfWork;
        _access = access;
        _links = links;
        _photos = photos;
        _actors = actors;
        _audit = audit;
        _probes = probes;
    }

    public async Task<ServiceResult<ProbeProjectResponse>> Handle(SaveProbeProjectCommand request, CancellationToken cancellationToken)
    {
        var probeId = request.ProbeId.Trim();
        if (await ProbeProjectGuard.RefuseAsync<ProbeProjectResponse>(_access, request.MemberKey, probeId, cancellationToken) is { } refused)
        {
            return refused;
        }

        var title = request.Title?.Trim() ?? string.Empty;
        var description = request.Description?.Trim() ?? string.Empty;
        var mentorName = Optional(request.DirectionMentorName);
        var mentorContact = Optional(request.DirectionMentorContact);

        if (Invalid(title, description, mentorName, mentorContact) is { } invalid)
        {
            return ServiceResult<ProbeProjectResponse>.Failure(ResultType.BadRequest, invalid.Code, invalid.Message);
        }

        var imageKeys = ProbeProjectMarkdown.ImageKeys(description);
        if (imageKeys is null)
        {
            return ForeignImage();
        }

        if (imageKeys.Distinct().Count() > ProbeProjectRules.MaxImagesPerProject)
        {
            return ServiceResult<ProbeProjectResponse>.Failure(
                ResultType.BadRequest, "TooManyImages", $"A project holds at most {ProbeProjectRules.MaxImagesPerProject} pictures.");
        }

        ProbeProject project;
        string? titleBefore = null;
        var isNew = request.ProjectKey is null;
        if (isNew)
        {
            var existing = await _unitOfWork.ProbeProjects.GetForMemberProbeAsync(request.MemberKey, probeId, cancellationToken);
            if (existing.Count >= ProbeProjectRules.MaxProjects)
            {
                return ServiceResult<ProbeProjectResponse>.Failure(
                    ResultType.BadRequest, "TooManyProjects", $"The probe holds at most {ProbeProjectRules.MaxProjects} projects.");
            }

            // A new project has no pictures yet: they are uploaded into a project that exists.
            if (imageKeys.Count > 0)
            {
                return ForeignImage();
            }

            project = new ProbeProject { MemberKey = request.MemberKey, ProbeId = probeId };
            _unitOfWork.ProbeProjects.Create(project, cancellationToken);
        }
        else
        {
            var found = await _unitOfWork.ProbeProjects.GetWithImagesAsync(request.ProjectKey!.Value, cancellationToken);
            if (found is null || found.MemberKey != request.MemberKey || found.ProbeId != probeId)
            {
                return new ServiceResult<ProbeProjectResponse>(ResultType.NotFound);
            }

            project = found;
            titleBefore = project.Title;
            var own = project.Images.Select(image => image.ProbeProjectImageKey).ToHashSet();
            if (imageKeys.Any(key => !own.Contains(key)))
            {
                return ForeignImage();
            }
        }

        var actor = await _actors.ResolveAsync(cancellationToken);
        project.Title = title;
        project.IsIndividual = request.IsIndividual;
        project.Description = description;
        project.DirectionMentorName = mentorName;
        project.DirectionMentorContact = mentorContact;
        project.UpdatedDate = DateTime.UtcNow;
        project.UpdatedByUserKey = actor.UserKey;
        project.UpdatedByName = actor.Name;

        var shown = imageKeys.ToHashSet();
        var dropped = project.Images.Where(image => !shown.Contains(image.ProbeProjectImageKey)).ToList();
        _unitOfWork.ProbeProjects.RemoveImages(dropped);
        foreach (var image in dropped)
        {
            project.Images.Remove(image);
        }

        await _audit.RecordAsync(
            new AuditRecord(
                AuditArea.Probe,
                isNew ? AuditActions.Probe.ProjectAdded : AuditActions.Probe.ProjectEdited,
                project.ProbeProjectKey,
                ProgressAuditLabels.Probe(_probes, probeId),
                request.MemberKey,
                isNew ? AuditChanges.Added(("project", title)) : AuditChanges.Of(("project", titleBefore, title))),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // After the save, so a failed save never leaves the text pointing at deleted files. A blob
        // missed here is swept by the orphan cleanup: no row names it any more.
        foreach (var image in dropped)
        {
            await _photos.DeletePhotoAsync(image.BlobName, cancellationToken);
        }

        return new ServiceResult<ProbeProjectResponse>(
            isNew ? ResultType.Created : ResultType.Success,
            ProbeProjectMapping.ToResponse(project, _links));
    }

    private static ServiceResult<ProbeProjectResponse> ForeignImage() =>
        ServiceResult<ProbeProjectResponse>.Failure(
            ResultType.BadRequest, "ForeignImage", "Pictures must be uploaded into the project.");

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static (string Code, string Message)? Invalid(string title, string description, string? mentorName, string? mentorContact)
    {
        if (title.Length == 0)
        {
            return ("TitleRequired", "A project needs a title.");
        }

        if (title.Length > ProbeProjectRules.MaxTitleLength)
        {
            return ("TitleTooLong", $"The title holds at most {ProbeProjectRules.MaxTitleLength} characters.");
        }

        if (description.Length > ProbeProjectRules.MaxDescriptionLength)
        {
            return ("DescriptionTooLong", $"The description holds at most {ProbeProjectRules.MaxDescriptionLength} characters.");
        }

        if (mentorName?.Length > ProbeProjectRules.MaxMentorNameLength || mentorContact?.Length > ProbeProjectRules.MaxMentorContactLength)
        {
            return ("MentorTooLong", $"The mentor's name and contact hold at most {ProbeProjectRules.MaxMentorNameLength} characters each.");
        }

        return null;
    }
}
