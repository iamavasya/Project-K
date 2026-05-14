using MediatR;
using ProjectK.Common.Models.Dtos.UserModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command
{
    public record ConfirmAccountEmailChangeCommand(Guid UserKey, string Email, string Token)
        : IRequest<ServiceResult<AccountSettingsDto>>;
}
