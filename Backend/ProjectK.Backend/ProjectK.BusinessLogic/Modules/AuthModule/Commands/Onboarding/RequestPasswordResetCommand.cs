using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding
{
    public record RequestPasswordResetCommand(string Email) : IRequest<ServiceResult<bool>>;
}
