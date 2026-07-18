using MediatR;
using ProjectK.Common.Models.Records;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Setup
{
    public record InitializeSetupCommand(
        string Email,
        string Password,
        string FirstName,
        string LastName,
        bool EnforcePrivilegedMfa = false,
        bool SeedDemoData = false
    ) : IRequest<ServiceResult<LoginUserResponse>>;
}
