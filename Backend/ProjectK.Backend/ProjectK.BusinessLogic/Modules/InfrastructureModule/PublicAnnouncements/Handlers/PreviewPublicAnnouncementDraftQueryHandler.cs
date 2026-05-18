using MediatR;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Queries;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Handlers;

public sealed class PreviewPublicAnnouncementDraftQueryHandler
    : IRequestHandler<PreviewPublicAnnouncementDraftQuery, ServiceResult<PublicAnnouncementPreviewDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPublicAnnouncementRenderer _renderer;

    public PreviewPublicAnnouncementDraftQueryHandler(
        IUnitOfWork unitOfWork,
        IPublicAnnouncementRenderer renderer)
    {
        _unitOfWork = unitOfWork;
        _renderer = renderer;
    }

    public async Task<ServiceResult<PublicAnnouncementPreviewDto>> Handle(
        PreviewPublicAnnouncementDraftQuery request,
        CancellationToken cancellationToken)
    {
        var draft = await _unitOfWork.PublicAnnouncements.GetByKeyAsync(request.DraftKey, cancellationToken);
        if (draft == null || draft.Status == PublicAnnouncementStatus.Deleted)
        {
            return ServiceResult<PublicAnnouncementPreviewDto>.Failure(ResultType.NotFound, "DraftNotFound", "Announcement draft not found.");
        }

        return new ServiceResult<PublicAnnouncementPreviewDto>(ResultType.Success, _renderer.Render(draft));
    }
}
