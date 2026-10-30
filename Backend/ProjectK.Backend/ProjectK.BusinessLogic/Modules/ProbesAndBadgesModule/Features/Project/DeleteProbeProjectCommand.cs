using MediatR;
using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Audit;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Features.Project;

/// <summary>Removes a project with its pictures, blobs included.</summary>
public sealed record DeleteProbeProjectCommand(Guid MemberKey, string ProbeId, Guid ProjectKey) : IRequest<ServiceResult<bool>>;

public sealed class DeleteProbeProjectCommandHandler : IRequestHandler<DeleteProbeProjectCommand, ServiceResult<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProbeProjectAccess _access;
    private readonly IPhotoService _photos;
    private readonly IKurinAudit _audit;
    private readonly IProbesCatalogService _probes;

    public DeleteProbeProjectCommandHandler(
        IUnitOfWork unitOfWork,
        IProbeProjectAccess access,
        IPhotoService photos,
        IKurinAudit audit,
        IProbesCatalogService probes)
    {
        _unitOfWork = unitOfWork;
        _access = access;
        _photos = photos;
        _audit = audit;
        _probes = probes;
    }

    public async Task<ServiceResult<bool>> Handle(DeleteProbeProjectCommand request, CancellationToken cancellationToken)
    {
        var probeId = request.ProbeId.Trim();
        if (await ProbeProjectGuard.RefuseAsync<bool>(_access, request.MemberKey, probeId, cancellationToken) is { } refused)
        {
            return refused;
        }

        var project = await _unitOfWork.ProbeProjects.GetWithImagesAsync(request.ProjectKey, cancellationToken);
        if (project is null || project.MemberKey != request.MemberKey || project.ProbeId != probeId)
        {
            return new ServiceResult<bool>(ResultType.NotFound);
        }

        var blobs = project.Images.Select(image => image.BlobName).ToList();
        _unitOfWork.ProbeProjects.Delete(project, cancellationToken);

        await _audit.RecordAsync(
            new AuditRecord(
                AuditArea.Probe,
                AuditActions.Probe.ProjectRemoved,
                project.ProbeProjectKey,
                ProgressAuditLabels.Probe(_probes, probeId),
                request.MemberKey,
                AuditChanges.Removed(("project", project.Title))),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var blob in blobs)
        {
            await _photos.DeletePhotoAsync(blob, cancellationToken);
        }

        return new ServiceResult<bool>(ResultType.Success, true);
    }
}
