using MediatR;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Queries;

public record PreviewPublicAnnouncementDraftQuery(Guid DraftKey)
    : IRequest<ServiceResult<PublicAnnouncementPreviewDto>>;
