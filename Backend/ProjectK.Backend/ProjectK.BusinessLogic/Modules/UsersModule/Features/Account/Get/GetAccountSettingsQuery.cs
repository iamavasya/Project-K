using MediatR;
using ProjectK.Common.Models.Dtos.UsersModule;
using ProjectK.Common.Models.Records;
using ProjectK.Common.Models.Dtos.UsersModule;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Features.Account.Get
{
    public record GetAccountSettingsQuery(Guid UserKey) : IRequest<ServiceResult<AccountSettingsDto>>;
}
