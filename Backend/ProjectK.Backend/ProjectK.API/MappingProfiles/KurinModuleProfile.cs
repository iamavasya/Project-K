using AutoMapper;
using AutoMapper.EquivalencyExpression;
using ProjectK.API.MappingProfiles.Resolvers;

using ProjectK.BusinessLogic.Modules.KurinModule.Features.Group.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Leadership.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.PlanningSession.Create;

using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Entities.KurinModule.Planning;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Dtos.Requests;
using ProjectK.Common.Models.Records;

namespace ProjectK.API.MappingProfiles
{
    public class KurinModuleProfile : Profile
    {
        public KurinModuleProfile()
        {
            // Kurin Mapping
            CreateMap<Kurin, KurinResponse>();
            CreateMap<UpsertKurin, Kurin>()
                .ForMember(dest => dest.KurinKey, opt => opt.Ignore())
                .ForMember(dest => dest.Number, opt => opt.MapFrom(src => src.Number))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.Now));

            // Group Mapping
            CreateMap<Group, GroupResponse>()
                .ForMember(dest => dest.GroupKey, opt => opt.MapFrom(src => src.GroupKey))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.KurinKey, opt => opt.MapFrom(src => src.KurinKey))
                .ForMember(dest => dest.KurinNumber, opt => opt.MapFrom(src => src.Kurin.Number));
            CreateMap<UpsertGroup, Group>()
                .ForMember(dest => dest.GroupKey, opt => opt.Ignore())
                .ForMember(dest => dest.KurinKey, opt => opt.Ignore())
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.Now));

            // Member Mapping
            CreateMap<UpsertMember, Member>()
                .ForMember(dest => dest.MemberKey, opt => opt.Ignore())
                .ForMember(dest => dest.GroupKey, opt => opt.MapFrom(src => src.GroupKey))
                .ForMember(dest => dest.KurinKey, opt => opt.Ignore())
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
                .ForMember(dest => dest.DateOfBirth, opt => opt.MapFrom(src => src.DateOfBirth))
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.PlastLevelHistory, opt => opt.Ignore());

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
                .ForMember(dest => dest.PlastLevelHistories, opt => opt.MapFrom(src => src.PlastLevelHistory))
                .ForMember(d => d.ProfilePhotoUrl, opt => opt.MapFrom<ProfilePhotoUrlResolver>());

            CreateMap<Member, MemberLookupDto>()
                .ForMember(dest => dest.MemberKey, opt => opt.MapFrom(src => src.MemberKey))
                .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
                .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
                .ForMember(dest => dest.MiddleName, opt => opt.MapFrom(src => src.MiddleName));

            // Plast Level History Mapping
            CreateMap<PlastLevelHistory, PlastLevelHistoryDto>();

            // Leadership History Mapping
            CreateMap<LeadershipHistory, LeadershipHistoryDto>()
                .ForMember(dest => dest.LeadershipHistoryKey, opt => opt.MapFrom(src => src.LeadershipHistoryKey));

            CreateMap<LeadershipHistory, LeadershipHistoryMemberDto>()
                .ForMember(dest => dest.LeadershipKey, opt => opt.Ignore())
                .ForMember(dest => dest.Member, opt => opt.MapFrom(src => src.Member))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role));

            CreateMap<LeadershipHistoryMemberDto, LeadershipHistory>()
                .EqualityComparison((src, dest) => src.LeadershipHistoryKey == dest.LeadershipHistoryKey)
                .ForMember(dest => dest.LeadershipHistoryKey, opt => opt.Ignore())
                .ForMember(dest => dest.Leadership, opt => opt.Ignore())
                .ForMember(dest => dest.MemberKey, opt => opt.MapFrom(src => src.Member.MemberKey))
                .ForMember(dest => dest.Member, opt => opt.Ignore())
                .ForMember(dest => dest.LeadershipKey, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate));

            // Leadership Mapping
            CreateMap<Leadership, LeadershipDto>()
                .ForMember(dest => dest.LeadershipKey, opt => opt.MapFrom(src => src.LeadershipKey))
                .ForMember(dest => dest.LeadershipHistories, opt => opt.MapFrom(src => src.LeadershipHistories))
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
                .ForMember(dest => dest.KurinKey, opt => opt.MapFrom(src => src.KurinKey))
                .ForMember(dest => dest.GroupKey, opt => opt.MapFrom(src => src.GroupKey));

            CreateMap<UpsertLeadership, Leadership>()
                .ForMember(dest => dest.LeadershipKey, opt => opt.Ignore())
                .ForMember(dest => dest.Type, opt => opt.Ignore())
                .ForMember(dest => dest.KurinKey, opt => opt.Ignore())
                .ForMember(dest => dest.GroupKey, opt => opt.Ignore())
                .ForMember(dest => dest.StartDate, opt => opt.MapFrom(src => src.StartDate))
                .ForMember(dest => dest.EndDate, opt => opt.MapFrom(src => src.EndDate))
                .ForMember(dest => dest.LeadershipHistories, opt => opt.MapFrom(src => src.LeadershipHistoryMembers));

            // Planning Mapping
            CreateMap<CreatePlanningSession, PlanningSession>()
                .ForMember(dest => dest.IsCalculated, opt => opt.MapFrom(src => false))

                .ForMember(dest => dest.OptimalStartDate, opt => opt.Ignore())
                .ForMember(dest => dest.OptimalEndDate, opt => opt.Ignore())
                .ForMember(dest => dest.ConflictScore, opt => opt.Ignore());

            CreateMap<ParticipantInputDto, PlanningParticipant>()
                .ForMember(dest => dest.BusyRanges, opt => opt.MapFrom(src => src.BusyRanges));

            CreateMap<DateRangeDto, ParticipantBusyRange>();

            // Entity -> Response DTO
            CreateMap<PlanningSession, PlanningSessionDto>();
            CreateMap<PlanningParticipant, PlanningParticipantDto>();
            CreateMap<ParticipantBusyRange, DateRangeDto>();
        }
    }
}
