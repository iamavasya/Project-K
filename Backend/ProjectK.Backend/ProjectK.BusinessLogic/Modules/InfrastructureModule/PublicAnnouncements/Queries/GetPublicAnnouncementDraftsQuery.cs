using MediatR;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Queries;

public record GetPublicAnnouncementDraftsQuery(PublicAnnouncementStatus? Status)
    : IRequest<ServiceResult<IReadOnlyCollection<PublicAnnouncementDraftDto>>>;
