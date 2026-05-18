using ProjectK.Common.Models.Enums;

namespace ProjectK.Common.Models.Dtos.InfrastructureModule;

public record PublicAnnouncementPreviewDto(
    string RenderedText,
    PublicAnnouncementParseMode ParseMode,
    bool WillSendAsPhoto,
    bool RequiresSeparateTextMessage,
    IReadOnlyCollection<string> Warnings);
