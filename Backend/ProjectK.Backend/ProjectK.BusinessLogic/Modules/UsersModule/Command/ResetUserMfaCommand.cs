using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command
{
    public record ResetUserMfaCommand(Guid TargetUserKey) : IRequest<ServiceResult<bool>>;
}
