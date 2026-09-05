using MediatR;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.Features.PublicAnnouncement.Publish;

public record PublishPublicAnnouncementDraftCommand(Guid DraftKey)
    : IRequest<ServiceResult<PublicAnnouncementDraftDto>>;
