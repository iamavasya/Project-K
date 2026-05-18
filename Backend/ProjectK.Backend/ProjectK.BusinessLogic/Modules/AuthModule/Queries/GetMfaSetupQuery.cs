using MediatR;
using ProjectK.Common.Models.Dtos.AuthModule;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Queries
{
    public record GetMfaSetupQuery(Guid UserKey) : IRequest<ServiceResult<MfaSetupResponseDto>>;
}
