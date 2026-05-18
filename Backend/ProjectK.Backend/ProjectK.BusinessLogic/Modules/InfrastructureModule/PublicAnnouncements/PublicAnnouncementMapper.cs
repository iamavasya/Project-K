using ProjectK.Common.Entities.InfrastructureModule;
using ProjectK.Common.Models.Dtos.InfrastructureModule;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements;

internal static class PublicAnnouncementMapper
{
    public static PublicAnnouncementDraftDto ToDto(PublicAnnouncementDraft draft)
    {
        return new PublicAnnouncementDraftDto(
            draft.PublicAnnouncementDraftKey,
            draft.Status,
            draft.SourceType,
            draft.SourceId,
            draft.SourceUrl,
            draft.Environment,
            draft.Version,
            draft.Codename,
            draft.Title,
            draft.Body,
            draft.RenderedText,
            draft.ParseMode,
            draft.ImageBlobKey,
            draft.ImageUrl,
            draft.ImageAltText,
            draft.ImagePlacement,
            draft.TemplateKey,
            draft.TemplateDataJson,
            draft.CreatedByUserKey,
            draft.UpdatedByUserKey,
            draft.ApprovedByUserKey,
            draft.PublishedByUserKey,
            draft.CreatedAtUtc,
            draft.UpdatedAtUtc,
            draft.ApprovedAtUtc,
            draft.PublishedAtUtc,
            draft.TelegramMessageId,
            draft.LastPublishError);
    }
}
