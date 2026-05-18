using ProjectK.Common.Entities.InfrastructureModule;

namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule;

public interface IPublicAnnouncementPublisher
{
    Task<PublicAnnouncementPublishResult> PublishAsync(
        PublicAnnouncementDraft draft,
        string renderedText,
        CancellationToken cancellationToken = default);
}
