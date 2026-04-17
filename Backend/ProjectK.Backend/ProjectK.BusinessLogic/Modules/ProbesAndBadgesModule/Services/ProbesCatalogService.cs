using ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Models;
using ProjectK.ProbeAndBadges.Abstractions;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services
{
    public sealed class ProbesCatalogService : IProbesCatalogService
    {
        private readonly IProbesCatalog _probesCatalog;

        public ProbesCatalogService(IProbesCatalog probesCatalog)
        {
            _probesCatalog = probesCatalog;
        }

        public IReadOnlyList<ProbeSummaryResponse> GetProbes()
        {
            return _probesCatalog
                .GetAll()
                .Select(ProbeSummaryResponse.FromProbe)
                .ToList();
        }

        public GroupedProbeResponse? GetGroupedProbeById(string probeId)
        {
            var probe = _probesCatalog.GetById(probeId);
            return probe is null ? null : GroupedProbeResponse.FromProbe(probe);
        }
    }
}
