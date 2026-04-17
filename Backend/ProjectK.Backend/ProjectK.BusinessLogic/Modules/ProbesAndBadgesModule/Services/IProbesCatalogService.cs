using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services
{
    public interface IProbesCatalogService
    {
        IReadOnlyList<ProbeSummaryResponse> GetProbes();

        GroupedProbeResponse? GetGroupedProbeById(string probeId);
    }
}
