using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command
{
    public record DisableOwnMfaCommand(Guid UserKey, string CurrentPassword) : IRequest<ServiceResult<bool>>;
}
