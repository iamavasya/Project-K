using MediatR;
using ProjectK.BusinessLogic.Modules.UsersModule.Models;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Features.User.Get
{
    public record GetUserQuery(Guid UserKey) : IRequest<ServiceResult<UserDto>>;
}
