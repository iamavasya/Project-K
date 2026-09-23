using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Records;
using ProjectK.ProbeAndBadges.Abstractions;

namespace ProjectK.BusinessLogic.Modules.ProbesAndBadgesModule.Services;

/// <summary>Answers <see cref="IProgressCatalogReader"/> straight from the catalogue package.</summary>
public sealed class ProgressCatalogReader : IProgressCatalogReader
{
    private readonly IProbesCatalog _probes;
    private readonly IBadgesCatalog _badges;

    public ProgressCatalogReader(IProbesCatalog probes, IBadgesCatalog badges)
    {
        _probes = probes;
        _badges = badges;
    }

    public IReadOnlyList<CatalogProbe> GetProbes()
    {
        return _probes
            .GetAll()
            .Select(probe => new CatalogProbe(
                probe.Id,
                probe.Title,
                probe.Points.Select(point => point.Id).ToList()))
            .ToList();
    }

    public IReadOnlyList<string> GetBadgeIds()
    {
        return _badges
            .GetAll()
            .Select(badge => badge.Id)
            .ToList();
    }
}
