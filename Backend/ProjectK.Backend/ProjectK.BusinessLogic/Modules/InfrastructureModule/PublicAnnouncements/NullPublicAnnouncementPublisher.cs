using Microsoft.Extensions.Logging;
using ProjectK.Common.Entities.InfrastructureModule;
using ProjectK.Common.Interfaces.Modules.InfrastructureModule;

namespace ProjectK.BusinessLogic.Modules.InfrastructureModule.PublicAnnouncements;

public sealed class NullPublicAnnouncementPublisher : IPublicAnnouncementPublisher
{
    private readonly ILogger<NullPublicAnnouncementPublisher> _logger;

    public NullPublicAnnouncementPublisher(ILogger<NullPublicAnnouncementPublisher> logger)
    {
        _logger = logger;
    }

    public Task<PublicAnnouncementPublishResult> PublishAsync(
        PublicAnnouncementDraft draft,
        string renderedText,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Public announcement dry-run publish. DraftKey={DraftKey}, Title={Title}, TextLength={TextLength}",
            draft.PublicAnnouncementDraftKey,
            draft.Title,
            renderedText.Length);

        return Task.FromResult(PublicAnnouncementPublishResult.Success($"dry-run:{draft.PublicAnnouncementDraftKey}"));
    }
}
