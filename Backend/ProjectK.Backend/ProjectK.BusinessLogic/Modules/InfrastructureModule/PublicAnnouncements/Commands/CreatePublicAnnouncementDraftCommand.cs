using MediatR;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements.Commands;

public record CreatePublicAnnouncementDraftCommand(
    string Title,
    string Body,
    PublicAnnouncementSourceType SourceType,
    string? SourceId,
    string? SourceUrl,
    string? Environment,
    string? Version,
    string? Codename,
    PublicAnnouncementParseMode ParseMode,
    string? ImageBlobKey,
    string? ImageUrl,
    string? ImageAltText,
    PublicAnnouncementImagePlacement ImagePlacement,
    string? TemplateKey,
    string? TemplateDataJson) : IRequest<ServiceResult<PublicAnnouncementDraftDto>>;
