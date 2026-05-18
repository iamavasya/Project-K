using MediatR;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.User
{
    public record VerifyMfaLoginCommand(string Email, string Code, bool RememberMe) : IRequest<ServiceResult<LoginUserResponse>>;
}
