using MediatR;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Queries;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Handlers;

public sealed class GetPublicAnnouncementDraftQueryHandler
    : IRequestHandler<GetPublicAnnouncementDraftQuery, ServiceResult<PublicAnnouncementDraftDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPublicAnnouncementDraftQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<PublicAnnouncementDraftDto>> Handle(
        GetPublicAnnouncementDraftQuery request,
        CancellationToken cancellationToken)
    {
        var draft = await _unitOfWork.PublicAnnouncements.GetByKeyAsync(request.DraftKey, cancellationToken);
        if (draft == null || draft.Status == PublicAnnouncementStatus.Deleted)
        {
            return ServiceResult<PublicAnnouncementDraftDto>.Failure(ResultType.NotFound, "DraftNotFound", "Announcement draft not found.");
        }

        return new ServiceResult<PublicAnnouncementDraftDto>(ResultType.Success, PublicAnnouncementMapper.ToDto(draft));
    }
}
