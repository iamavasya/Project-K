using MediatR;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.ActivateAccount;

/// <summary>
/// Turns an invitation into a usable account and answers with a signed-in session, so the person
/// lands inside the app rather than on the sign-in form they have never used.
/// </summary>
public record ActivateAccountCommand(string Token, string Password) : IRequest<ServiceResult<LoginUserResponse>>;
