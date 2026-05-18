using MediatR;
using ProjectK.Common.Models.Dtos.UserModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Queries
{
    public record GetAccountSettingsQuery(Guid UserKey) : IRequest<ServiceResult<AccountSettingsDto>>;
}
