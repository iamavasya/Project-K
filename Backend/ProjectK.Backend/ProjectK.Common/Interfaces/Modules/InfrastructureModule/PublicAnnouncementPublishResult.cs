namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule;

public sealed record PublicAnnouncementPublishResult(
    bool Succeeded,
    string? TelegramMessageId = null,
    string? ErrorMessage = null,
    bool PartiallySucceeded = false)
{
    public static PublicAnnouncementPublishResult Success(string? telegramMessageId = null) =>
        new(true, telegramMessageId);

    public static PublicAnnouncementPublishResult Failure(
        string errorMessage,
        string? telegramMessageId = null,
        bool partiallySucceeded = false) =>
        new(false, telegramMessageId, errorMessage, partiallySucceeded);
}
