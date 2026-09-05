using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Settings.Update
{
    public record UpdateSystemSettingCommand(string Key, string Value) : IRequest<ServiceResult<object>>;
}
