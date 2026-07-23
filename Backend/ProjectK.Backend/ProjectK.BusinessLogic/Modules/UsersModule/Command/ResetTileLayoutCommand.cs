using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.UsersModule.Command
{
    public record ResetTileLayoutCommand(Guid UserKey, string BoardKey) : IRequest<ServiceResult<bool>>;
}
