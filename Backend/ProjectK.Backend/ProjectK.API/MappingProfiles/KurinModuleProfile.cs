using AutoMapper;
using ProjectK.BusinessLogic.Modules.Kurin.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Groups;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Kurins;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;

namespace ProjectK.API.MappingProfiles
{
    public class KurinModuleProfile : Profile
    {
        public KurinModuleProfile()
        {
            // Kurin Mapping
            CreateMap<Kurin, KurinResponse>();
            CreateMap<UpsertKurinCommand, Kurin>()
                .ForMember(dest => dest.KurinKey, opt => opt.Ignore())
                .ForMember(dest => dest.Number, opt => opt.MapFrom(src => src.Number))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.Now));

            // Group Mapping
            CreateMap<Group, GroupResponse>()
                .ForMember(dest => dest.GroupKey, opt => opt.MapFrom(src => src.GroupKey))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.KurinKey, opt => opt.MapFrom(src => src.KurinKey))
                .ForMember(dest => dest.KurinNumber, opt => opt.MapFrom(src => src.Kurin.Number));
            CreateMap<UpsertGroupCommand, Group>()
                .ForMember(dest => dest.GroupKey, opt => opt.Ignore())
                .ForMember(dest => dest.KurinKey, opt => opt.Ignore()   )
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.Now));
        }
    }
}
