namespace ProjectK.BusinessLogic.Modules.AuthModule.Models;

/// <summary>
/// What the dev role switcher gets: a sign-in as the chosen account, plus the ticket that brings
/// the administrator back without a password.
/// </summary>
public record DevImpersonationResponse(LoginUserResponse Login, string ReturnTicket, string Role, Guid KurinKey);
