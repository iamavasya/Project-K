using ProjectK.Common.Entities.InfrastructureModule;
using ProjectK.Common.Models.Dtos.InfrastructureModule;

namespace ProjectK.Common.Interfaces.Modules.InfrastructureModule;

public interface IPublicAnnouncementRenderer
{
    PublicAnnouncementPreviewDto Render(PublicAnnouncementDraft draft);
}
