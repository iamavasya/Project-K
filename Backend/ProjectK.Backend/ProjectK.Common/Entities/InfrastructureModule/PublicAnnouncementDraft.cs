using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Entities.InfrastructureModule;

public class PublicAnnouncementDraft
{
    public Guid PublicAnnouncementDraftKey { get; set; } = Guid.NewGuid();

    public PublicAnnouncementStatus Status { get; set; } = PublicAnnouncementStatus.Draft;
    public PublicAnnouncementSourceType SourceType { get; set; } = PublicAnnouncementSourceType.Manual;
    public string? SourceId { get; set; }
    public string? SourceUrl { get; set; }
    public string? Environment { get; set; }
    public string? Version { get; set; }
    public string? Codename { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? RenderedText { get; set; }
    public PublicAnnouncementParseMode ParseMode { get; set; } = PublicAnnouncementParseMode.PlainText;
    public string? ImageBlobKey { get; set; }
    public string? ImageUrl { get; set; }
    public string? ImageAltText { get; set; }
    public PublicAnnouncementImagePlacement ImagePlacement { get; set; } = PublicAnnouncementImagePlacement.ImageFirst;
    public string? TemplateKey { get; set; }
    public string? TemplateDataJson { get; set; }

    public Guid? CreatedByUserKey { get; set; }
    public Guid? UpdatedByUserKey { get; set; }
    public Guid? ApprovedByUserKey { get; set; }
    public Guid? PublishedByUserKey { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? ApprovedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public string? TelegramMessageId { get; set; }
    public string? LastPublishError { get; set; }
}
