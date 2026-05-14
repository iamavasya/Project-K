using MediatR;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Commands.User
{
    public record GenerateMfaRecoveryCodesCommand(Guid UserKey, string CurrentPassword)
        : IRequest<ServiceResult<MfaRecoveryCodesResponseDto>>;
}
