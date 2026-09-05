using AutoMapper;
using MediatR;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.Features.PublicAnnouncement.Get;

public sealed class GetPublicAnnouncementDraftsQueryHandler
    : IRequestHandler<GetPublicAnnouncementDraftsQuery, ServiceResult<IReadOnlyCollection<PublicAnnouncementDraftDto>>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPublicAnnouncementDraftsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<ServiceResult<IReadOnlyCollection<PublicAnnouncementDraftDto>>> Handle(
        GetPublicAnnouncementDraftsQuery request,
        CancellationToken cancellationToken)
    {
        var drafts = await _unitOfWork.PublicAnnouncements.GetByStatusAsync(request.Status, cancellationToken);
        var result = drafts.Select(_mapper.Map<PublicAnnouncementDraftDto>).ToList();
        return new ServiceResult<IReadOnlyCollection<PublicAnnouncementDraftDto>>(ProjectK.Common.Models.Enums.ResultType.Success, result);
    }
}
