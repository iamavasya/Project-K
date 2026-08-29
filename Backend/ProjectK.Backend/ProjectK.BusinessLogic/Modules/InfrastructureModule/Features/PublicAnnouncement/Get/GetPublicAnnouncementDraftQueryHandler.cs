using AutoMapper;
using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.Features.PublicAnnouncement.Get;

public sealed class GetPublicAnnouncementDraftQueryHandler
    : IRequestHandler<GetPublicAnnouncementDraftQuery, ServiceResult<PublicAnnouncementDraftDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPublicAnnouncementDraftQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
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

        return new ServiceResult<PublicAnnouncementDraftDto>(ResultType.Success, _mapper.Map<PublicAnnouncementDraftDto>(draft));
    }
}
