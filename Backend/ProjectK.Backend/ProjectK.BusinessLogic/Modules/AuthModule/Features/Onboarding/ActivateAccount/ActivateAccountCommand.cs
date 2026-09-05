using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ActivateAccount
{
    public record ActivateAccountCommand(string Token, string Password) : IRequest<ServiceResult<Guid>>;
}
