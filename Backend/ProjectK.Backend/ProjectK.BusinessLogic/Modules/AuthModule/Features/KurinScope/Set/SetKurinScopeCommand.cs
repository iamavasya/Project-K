using MediatR;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.KurinScope.Set
{
    /// <param name="KurinKey">Kurin to step into, or null to return to system-wide scope.</param>
    public record SetKurinScopeCommand(Guid UserKey, Guid? KurinKey)
        : IRequest<ServiceResult<LoginUserResponse>>;
}
