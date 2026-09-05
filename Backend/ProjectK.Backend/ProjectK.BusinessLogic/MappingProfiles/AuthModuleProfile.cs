using AutoMapper;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Dtos.AuthModule.Requests;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.User.Login;
using ProjectK.BusinessLogic.Modules.AuthModule.Features.User.Register;

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
