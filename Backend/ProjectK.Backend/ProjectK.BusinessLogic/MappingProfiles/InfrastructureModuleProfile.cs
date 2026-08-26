using AutoMapper;
using ProjectK.Common.Entities.InfrastructureModule;
using ProjectK.Common.Models.Dtos.InfrastructureModule;

namespace ProjectK.BusinessLogic.MappingProfiles;

/// <summary>
/// Mappings for the infrastructure module. The announcement draft used to be copied by a hand-written
/// static mapper — a straight property-for-property copy, which is what AutoMapper is for.
/// </summary>
public sealed class InfrastructureModuleProfile : Profile
{
    public InfrastructureModuleProfile()
    {
        CreateMap<PublicAnnouncementDraft, PublicAnnouncementDraftDto>();
    }
}
