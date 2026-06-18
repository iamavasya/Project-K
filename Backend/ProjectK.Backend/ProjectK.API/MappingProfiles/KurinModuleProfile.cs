using AutoMapper;
using AutoMapper.EquivalencyExpression;
using ProjectK.API.MappingProfiles.Resolvers;

using ProjectK.BusinessLogic.Modules.KurinModule.Features.Group.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Kurin.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Leadership.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.Member.Upsert;
using ProjectK.BusinessLogic.Modules.KurinModule.Features.PlanningSession.Create;

using ProjectK.BusinessLogic.Modules.KurinModule.Models;
using ProjectK.Common.Entities.InfrastructureModule;
using ProjectK.Common.Entities.KurinModule;
using ProjectK.Common.Entities.KurinModule.Planning;
using ProjectK.Common.Models.Dtos;
using ProjectK.Common.Models.Dtos.Requests;
using ProjectK.Common.Models.Enums;
using ProjectK.Common.Models.Records;

namespace ProjectK.API.MappingProfiles
{
    public class KurinModuleProfile : Profile
    {
        public KurinModuleProfile()
        {
            // Kurin Mapping
            CreateMap<Kurin, KurinResponse>()
                .ForMember(dest => dest.IsZbtEnabled, opt => opt.MapFrom(src => src.IsZbtKurin))
                .ForMember(dest => dest.CurrentUserCount, opt => opt.MapFrom(src => src.Members.Count));
            CreateMap<UpsertKurin, Kurin>()
                .ForMember(dest => dest.KurinKey, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.Now));

            // Group Mapping
            CreateMap<Group, GroupResponse>()
                .ForMember(dest => dest.KurinNumber, opt => opt.MapFrom(src => src.Kurin.Number))
                .ForMember(dest => dest.SilhouetteUrl, opt => opt.MapFrom<GroupSilhouetteUrlResolver>());
            CreateMap<UpsertGroup, Group>()
                .ForMember(dest => dest.GroupKey, opt => opt.Ignore())
                .ForMember(dest => dest.KurinKey, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.Now));

            // Member Mapping
            CreateMap<UpsertMember, Member>()
                .ForMember(dest => dest.MemberKey, opt => opt.Ignore())
                .ForMember(dest => dest.UserKey, opt => opt.Ignore())
                .ForMember(dest => dest.KurinKey, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.PlastLevelHistory, opt => opt.Ignore());

            CreateMap<Member, MemberResponse>()
                .ForMember(dest => dest.LatestPlastLevel, opt => opt.MapFrom(src =>
                    src.PlastLevelHistory
                        .OrderByDescending(history => history.DateAchieved)
                        .Select(history => (PlastLevel?)history.PlastLevel)
                        .FirstOrDefault() ?? src.LatestPlastLevel))
                .ForMember(dest => dest.PlastLevelHistories, opt => opt.MapFrom(src => src.PlastLevelHistory))
                .ForMember(dest => dest.Warnings, opt => opt.MapFrom(src => src.MemberWarnings))
                .ForMember(dest => dest.Awards, opt => opt.MapFrom(src => src.MemberAwards))
                .ForMember(d => d.ProfilePhotoUrl, opt => opt.MapFrom<ProfilePhotoUrlResolver>());

            CreateMap<Member, MemberLookupDto>();

            // Plast Level History Mapping
            CreateMap<PlastLevelHistory, PlastLevelHistoryDto>();

            // Member Warning Mapping
            CreateMap<MemberWarning, MemberWarningDto>();

            // Notifications Mapping
            CreateMap<AppNotification, AppNotificationDto>();

            // Member Award Mapping
            CreateMap<MemberAward, MemberAwardDto>()
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src =>
                    $"/api/awards/images/{(int)src.Level}?colored={src.Status == ProjectK.Common.Models.Enums.BadgeProgressStatus.Confirmed}"));

            // Leadership History Mapping
            CreateMap<LeadershipHistory, LeadershipHistoryDto>()
                .ForMember(dest => dest.LeadershipType, opt => opt.MapFrom(src => src.Leadership.Type))
                .ForMember(dest => dest.GroupName, opt => opt.MapFrom(src => src.Leadership.Group != null ? src.Leadership.Group.Name : null));

            CreateMap<LeadershipHistory, LeadershipHistoryMemberDto>()
                .ForMember(dest => dest.LeadershipKey, opt => opt.Ignore());

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
            CreateMap<Leadership, LeadershipResponse>();

            CreateMap<UpsertLeadership, Leadership>()
                .ForMember(dest => dest.LeadershipKey, opt => opt.Ignore())
                .ForMember(dest => dest.Type, opt => opt.Ignore())
                .ForMember(dest => dest.KurinKey, opt => opt.Ignore())
                .ForMember(dest => dest.GroupKey, opt => opt.Ignore())
                .ForMember(dest => dest.LeadershipHistories, opt => opt.MapFrom(src => src.LeadershipHistoryMembers));

            // Planning Mapping
            CreateMap<CreatePlanningSession, PlanningSession>();

            CreateMap<ParticipantInputDto, PlanningParticipant>();

            CreateMap<DateRangeDto, ParticipantBusyRange>();

            // Entity -> Response DTO
            CreateMap<PlanningSession, PlanningSessionResponse>();
            CreateMap<PlanningParticipant, PlanningParticipantDto>();
            CreateMap<ParticipantBusyRange, DateRangeDto>();
        }
    }
}
