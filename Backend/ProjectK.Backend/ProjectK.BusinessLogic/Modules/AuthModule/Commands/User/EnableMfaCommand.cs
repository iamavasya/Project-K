using MediatR;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.User
{
    public record EnableMfaCommand(Guid UserKey, string Code) : IRequest<ServiceResult<MfaEnableResponseDto>>;
}
