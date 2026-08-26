using AutoMapper;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Dtos.AuthModule.Requests;

namespace ProjectK.BusinessLogic.MappingProfiles
{
    public class AuthModuleProfile : Profile
    {
        public AuthModuleProfile()
        {
            // Both destinations are completed by the handler: the command resolves KurinKey from
            // KurinNumber, and Identity owns most of AppUser.
            CreateMap<RegisterUserRequest, RegisterUserCommand>(MemberList.None);
            CreateMap<RegisterUserCommand, AppUser>(MemberList.None);

            CreateMap<LoginUserRequest, LoginUserCommand>();
        }
    }
}
