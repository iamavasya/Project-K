using System.Net;
using ProjectK.Common.Entities.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;
using ProjectK.Common.Models.Dtos.InfrastructureModule;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements;

public sealed class PublicAnnouncementRenderer : IPublicAnnouncementRenderer
{
    private const int TelegramTextLimit = 4096;
    private const int TelegramCaptionLimit = 1024;

    public PublicAnnouncementPreviewDto Render(PublicAnnouncementDraft draft)
    {
        var warnings = new List<string>();
        var rendered = RenderText(draft);
        var hasImage = !string.IsNullOrWhiteSpace(draft.ImageBlobKey)
            || !string.IsNullOrWhiteSpace(draft.ImageUrl);

        if (rendered.Length > TelegramTextLimit)
        {
            warnings.Add($"Rendered text exceeds Telegram message limit ({TelegramTextLimit}).");
        }

        if (hasImage && rendered.Length > TelegramCaptionLimit)
        {
            warnings.Add("Text is too long for a Telegram photo caption; publisher should send image and text separately.");
        }

        return new PublicAnnouncementPreviewDto(
            rendered,
            draft.ParseMode,
            hasImage,
            hasImage && rendered.Length > TelegramCaptionLimit,
            warnings);
    }

    private static string RenderText(PublicAnnouncementDraft draft)
    {
        var title = Normalize(draft.Title);
        var body = Normalize(draft.Body);

        if (draft.ParseMode == PublicAnnouncementParseMode.Html)
        {
            return $"<b>{WebUtility.HtmlEncode(title)}</b>\n\n{body}";
        }

        if (draft.ParseMode == PublicAnnouncementParseMode.MarkdownV2)
        {
            return $"*{EscapeMarkdownV2(title)}*\n\n{body}";
        }

        return $"{title}\n\n{body}";
    }

    private static string Normalize(string value)
    {
        return value.Trim().Replace("\r\n", "\n");
    }

    private static string EscapeMarkdownV2(string value)
    {
        var chars = new[] { '_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!' };
        var result = value;
        foreach (var ch in chars)
        {
            result = result.Replace(ch.ToString(), $"\\{ch}");
        }

        return result;
    }
}
