using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding
{
    public record ResetPasswordCommand(string Email, string Token, string NewPassword) : IRequest<ServiceResult<bool>>;
}
