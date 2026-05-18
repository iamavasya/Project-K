using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Dtos.InfrastructureModule;

public record PublicAnnouncementDraftDto(
    Guid PublicAnnouncementDraftKey,
    PublicAnnouncementStatus Status,
    PublicAnnouncementSourceType SourceType,
    string? SourceId,
    string? SourceUrl,
    string? Environment,
    string? Version,
    string? Codename,
    string Title,
    string Body,
    string? RenderedText,
    PublicAnnouncementParseMode ParseMode,
    string? ImageBlobKey,
    string? ImageUrl,
    string? ImageAltText,
    PublicAnnouncementImagePlacement ImagePlacement,
    string? TemplateKey,
    string? TemplateDataJson,
    Guid? CreatedByUserKey,
    Guid? UpdatedByUserKey,
    Guid? ApprovedByUserKey,
    Guid? PublishedByUserKey,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    DateTime? ApprovedAtUtc,
    DateTime? PublishedAtUtc,
    string? TelegramMessageId,
    string? LastPublishError);
