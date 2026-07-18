using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.Settings
{
    public record UpdateSystemSettingCommand(string Key, string Value) : IRequest<ServiceResult<object>>;
}
