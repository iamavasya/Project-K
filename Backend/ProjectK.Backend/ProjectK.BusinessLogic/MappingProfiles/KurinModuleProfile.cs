using AutoMapper;
using AutoMapper.EquivalencyExpression;
using ProjectK.BusinessLogic.MappingProfiles.Resolvers;

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

namespace ProjectK.BusinessLogic.MappingProfiles
{
    public class KurinModuleProfile : Profile
    {
        public KurinModuleProfile()
        {
            // Kurin Mapping
            CreateMap<Kurin, KurinResponse>()
                .ForMember(dest => dest.IsZbtEnabled, opt => opt.MapFrom(src => src.IsZbtKurin))
                .ForMember(dest => dest.CurrentUserCount, opt => opt.MapFrom(src => src.Members.Count));
            CreateMap<UpsertKurin, Kurin>(MemberList.None)
                .ForMember(dest => dest.KurinKey, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.UtcNow));

            // Group Mapping
            CreateMap<Group, GroupResponse>()
                .ForMember(dest => dest.KurinNumber, opt => opt.MapFrom(src => src.Kurin.Number))
                .ForMember(dest => dest.SilhouetteUrl, opt => opt.MapFrom<GroupSilhouetteUrlResolver>());
            CreateMap<UpsertGroup, Group>(MemberList.None)
                .ForMember(dest => dest.GroupKey, opt => opt.Ignore())
                .ForMember(dest => dest.KurinKey, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.UtcNow));

            // Member Mapping
            CreateMap<UpsertMember, Member>(MemberList.None)
                .ForMember(dest => dest.MemberKey, opt => opt.Ignore())
                .ForMember(dest => dest.UserKey, opt => opt.Ignore())
                .ForMember(dest => dest.KurinKey, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedDate, opt => opt.MapFrom(src => DateTime.UtcNow))
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
                .ForMember(d => d.ProfilePhotoUrl, opt => opt.MapFrom<ProfilePhotoUrlResolver>())
                // Offices live in LeadershipHistories, not on Member, so the entity cannot answer
                // this. The repository projections fill it; mapping from the entity leaves it null.
                .ForMember(dest => dest.UserRole, opt => opt.Ignore());

            // Lean list read model -> same response shape as the full card. Level,
            // active leadership and active warnings are already resolved in the
            // projection; history and awards are card-only and stay empty here.
            CreateMap<MemberListItemDto, MemberResponse>()
                .ForMember(dest => dest.PlastLevelHistories, opt => opt.Ignore())
                .ForMember(dest => dest.Awards, opt => opt.Ignore())
                .ForMember(dest => dest.ProfilePhotoUrl, opt => opt.MapFrom<MemberListItemPhotoUrlResolver>());

            CreateMap<Member, MemberLookupDto>()
                .ForMember(dest => dest.UserRole, opt => opt.Ignore());

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
            CreateMap<Leadership, LeadershipResponse>()
                .ForMember(dest => dest.EntityKey, opt => opt.MapFrom(src =>
                    src.Type == LeadershipType.Group
                        ? (src.GroupKey ?? Guid.Empty)
                        : (src.KurinKey ?? Guid.Empty)));

            CreateMap<UpsertLeadership, Leadership>(MemberList.None)
                .ForMember(dest => dest.LeadershipKey, opt => opt.Ignore())
                .ForMember(dest => dest.Type, opt => opt.Ignore())
                .ForMember(dest => dest.KurinKey, opt => opt.Ignore())
                .ForMember(dest => dest.GroupKey, opt => opt.Ignore())
                .ForMember(dest => dest.LeadershipHistories, opt => opt.MapFrom(src => src.LeadershipHistoryMembers));

            // Planning Mapping
            CreateMap<CreatePlanningSession, PlanningSession>(MemberList.None);

            CreateMap<ParticipantInputDto, PlanningParticipant>(MemberList.None);

            CreateMap<DateRangeDto, ParticipantBusyRange>(MemberList.None);

            // Entity -> Response DTO
            CreateMap<PlanningSession, PlanningSessionResponse>();
            CreateMap<PlanningParticipant, PlanningParticipantDto>();
            CreateMap<ParticipantBusyRange, DateRangeDto>();
        }
    }
}
