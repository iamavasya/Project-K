using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command
{
    public record ChangeOwnPasswordCommand(Guid UserKey, string CurrentPassword, string NewPassword)
        : IRequest<ServiceResult<bool>>;
}
