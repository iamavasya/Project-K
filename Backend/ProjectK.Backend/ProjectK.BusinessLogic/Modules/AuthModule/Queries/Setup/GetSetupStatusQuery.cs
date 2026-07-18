using MediatR;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Queries.Setup
{
    public record GetSetupStatusQuery() : IRequest<ServiceResult<SetupStatusResponse>>;

    public record SetupStatusResponse(bool IsInitialized);
}
