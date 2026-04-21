using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Onboarding
{
    public record ActivateAccountCommand(string Token, string Password) : IRequest<ServiceResult<Guid>>;
}
