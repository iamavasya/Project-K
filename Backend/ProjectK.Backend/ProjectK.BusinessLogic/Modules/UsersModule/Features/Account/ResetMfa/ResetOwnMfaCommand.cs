using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Features.Account.ResetMfa
{
    public record ResetOwnMfaCommand(Guid UserKey, string CurrentPassword) : IRequest<ServiceResult<bool>>;
}
