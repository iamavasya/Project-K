using MediatR;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.Features.PublicAnnouncement.Transition;

public record TransitionPublicAnnouncementDraftCommand(
    Guid DraftKey,
    PublicAnnouncementStatus TargetStatus) : IRequest<ServiceResult<PublicAnnouncementDraftDto>>;
