using MediatR;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.User.Login;

public class LoginUserCommand : IRequest<ServiceResult<LoginUserResponse>>
{
    public string Email { get; set; }
    public string Password { get; set; }

    /// <summary>
    /// The trust cookie the browser sent, when it finished the second factor on this device before.
    /// Read off the request by the controller, never posted in the body.
    /// </summary>
    public string? MfaTrustToken { get; set; }

    public LoginUserCommand(string email, string password)
    {
        Email = email;
        Password = password;
    }
}
