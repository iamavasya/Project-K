using MediatR;
using ProjectK.BusinessLogic.Modules.AuthModule.Models;
using ProjectK.Common.Models.Records;

namespace ProjectK.BusinessLogic.Modules.AuthModule.Features.Onboarding.GetStats
{
    public record GetOnboardingStatsQuery(Guid? KurinKey = null) : IRequest<ServiceResult<ZbtStatsDto>>;
}
