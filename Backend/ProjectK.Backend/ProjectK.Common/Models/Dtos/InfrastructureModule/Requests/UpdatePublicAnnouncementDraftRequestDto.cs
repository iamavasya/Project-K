using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Dtos.InfrastructureModule.Requests;

public record UpdatePublicAnnouncementDraftRequestDto(
    string Title,
    string Body,
    PublicAnnouncementParseMode ParseMode = PublicAnnouncementParseMode.PlainText,
    string? ImageBlobKey = null,
    string? ImageUrl = null,
    string? ImageAltText = null,
    PublicAnnouncementImagePlacement ImagePlacement = PublicAnnouncementImagePlacement.ImageFirst,
    string? TemplateKey = null,
    string? TemplateDataJson = null);
