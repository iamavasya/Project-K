using AutoMapper;
using ProjectK.API.MappingProfiles.Resolvers;
using ProjectK.BusinessLogic.Modules.Kurin.Models;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Groups;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Kurins;
using ProjectK.BusinessLogic.Modules.KurinModule.Commands.Members;
using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Infrastructure.Services;

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

            // Member Mapping
            CreateMap<UpsertMemberCommand, Member>()
                .ForMember(dest => dest.MemberKey, opt => opt.Ignore())
                .ForMember(dest => dest.GroupKey, opt => opt.MapFrom(src => src.GroupKey))
                .ForMember(dest => dest.KurinKey, opt => opt.Ignore())
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.Now));
            CreateMap<Member, MemberResponse>()
                .ForMember(dest => dest.MemberKey, opt => opt.MapFrom(src => src.MemberKey))
                .ForMember(dest => dest.GroupKey, opt => opt.MapFrom(src => src.GroupKey))
                .ForMember(dest => dest.KurinKey, opt => opt.MapFrom(src => src.KurinKey))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(d => d.ProfilePhotoUrl, opt => opt.MapFrom<ProfilePhotoUrlResolver>());
        }
    }
}
