using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Setup.Get
{
    public record GetSetupStatusQuery() : IRequest<ServiceResult<SetupStatusResponse>>;

    public record SetupStatusResponse(bool IsInitialized);
}
