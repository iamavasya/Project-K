using MediatR;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.User.VerifyMfaLogin
{
    /// <summary>
    /// Finishes a sign-in that stopped for the second factor. <paramref name="MfaToken"/> is the
    /// challenge the password step handed out for this account; the handler refuses without it.
    /// </summary>
    public record VerifyMfaLoginCommand(string Email, string Code, bool RememberMe, string? MfaToken = null)
        : IRequest<ServiceResult<LoginUserResponse>>;
}
