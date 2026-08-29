using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.RequestPasswordReset
{
    public record RequestPasswordResetCommand(string Email) : IRequest<ServiceResult<bool>>;
}
