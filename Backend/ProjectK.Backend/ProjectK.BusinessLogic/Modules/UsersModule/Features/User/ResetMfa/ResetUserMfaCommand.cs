using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Features.User.ResetMfa
{
    public record ResetUserMfaCommand(Guid TargetUserKey) : IRequest<ServiceResult<bool>>;
}
