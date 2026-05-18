using MediatR;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Commands;

public record PublishPublicAnnouncementDraftCommand(Guid DraftKey)
    : IRequest<ServiceResult<PublicAnnouncementDraftDto>>;
