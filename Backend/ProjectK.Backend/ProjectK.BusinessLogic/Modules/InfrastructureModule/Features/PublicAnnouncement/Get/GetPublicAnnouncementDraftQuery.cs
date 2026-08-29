using MediatR;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.Features.PublicAnnouncement.Get;

public record GetPublicAnnouncementDraftQuery(Guid DraftKey)
    : IRequest<ServiceResult<PublicAnnouncementDraftDto>>;
