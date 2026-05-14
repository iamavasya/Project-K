using MediatR;
using ProjectK.Common.Models.Dtos.UserModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command
{
    public record UpdateAccountProfileCommand(Guid UserKey, string Email, string? PhoneNumber, string? CurrentPassword = null)
        : IRequest<ServiceResult<AccountSettingsDto>>;
}
