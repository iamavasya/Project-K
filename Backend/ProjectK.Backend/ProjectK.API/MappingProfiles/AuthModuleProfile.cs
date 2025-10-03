using AutoMapper;
using ProjectK.BusinessLogic.Modules.AuthModule.Commands.User;
using ProjectK.Common.Entities.AuthModule;
using ProjectK.Common.Models.Dtos.AuthModule.Requests;

namespace ProjectK.API.MappingProfiles
{
    public class AuthModuleProfile : Profile
    {
        public AuthModuleProfile()
        {
            CreateMap<RegisterUserRequest, RegisterUserCommand>();
            CreateMap<RegisterUserCommand, AppUser>();

            CreateMap<LoginUserRequest, LoginUserCommand>();
        }
    }
}
