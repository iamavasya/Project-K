using MediatR;
using ProjectK.Common.Models.Dtos.UsersModule;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Dtos.UsersModule;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Features.Account.ConfirmEmailChange
{
    public record ConfirmAccountEmailChangeCommand(Guid UserKey, string Email, string Token)
        : IRequest<ServiceResult<AccountSettingsDto>>;
}
