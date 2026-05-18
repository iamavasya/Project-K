using MediatR;
using ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Queries;
using ProjectK.Common.Interfaces;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Handlers;

public sealed class GetPublicAnnouncementDraftsQueryHandler
    : IRequestHandler<GetPublicAnnouncementDraftsQuery, ServiceResult<IReadOnlyCollection<PublicAnnouncementDraftDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPublicAnnouncementDraftsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ServiceResult<IReadOnlyCollection<PublicAnnouncementDraftDto>>> Handle(
        GetPublicAnnouncementDraftsQuery request,
        CancellationToken cancellationToken)
    {
        var drafts = await _unitOfWork.PublicAnnouncements.GetByStatusAsync(request.Status, cancellationToken);
        var result = drafts.Select(PublicAnnouncementMapper.ToDto).ToList();
        return new ServiceResult<IReadOnlyCollection<PublicAnnouncementDraftDto>>(ProjectK.Common.Models.Enums.ResultType.Success, result);
    }
}
