using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Dtos.InfrastructureModule.Requests;

public record CreatePublicAnnouncementDraftRequestDto(
    string Title,
    string Body,
    PublicAnnouncementSourceType SourceType = PublicAnnouncementSourceType.Manual,
    string? SourceId = null,
    string? SourceUrl = null,
    string? Environment = null,
    string? Version = null,
    string? Codename = null,
    PublicAnnouncementParseMode ParseMode = PublicAnnouncementParseMode.PlainText,
    string? ImageBlobKey = null,
    string? ImageUrl = null,
    string? ImageAltText = null,
    PublicAnnouncementImagePlacement ImagePlacement = PublicAnnouncementImagePlacement.ImageFirst,
    string? TemplateKey = null,
    string? TemplateDataJson = null);
