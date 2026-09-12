using MediatR;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Setup.Initialize;

public record InitializeSetupCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    bool EnforcePrivilegedMfa = false,
    bool SeedDemoData = false
) : IRequest<ServiceResult<LoginUserResponse>>;
