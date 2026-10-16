using ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;
using ProjectK.Common.Models.Catalog;

namespace ProjectK.Infrastructure.Services.Catalog;

/// <summary>One fetched catalogue, ready to read. Replaced whole, never edited.</summary>
internal sealed class CatalogState
{
    public static readonly CatalogState Empty = new(
        0,
        new BadgesMetadata(string.Empty, string.Empty, string.Empty, null, string.Empty, false, string.Empty, 0),
        [],
        [],
        new Dictionary<int, CatalogImages>());

    public CatalogState(
        long revision,
        BadgesMetadata metadata,
        IReadOnlyList<Badge> badges,
        IReadOnlyList<Probe> probes,
        IReadOnlyDictionary<int, CatalogImages> awardImages)
    {
        Revision = revision;
        Metadata = metadata;
        Badges = badges;
        Probes = probes;
        AwardImages = awardImages;
        BadgesById = badges.GroupBy(b => b.Id).ToDictionary(g => g.Key, g => g.First());
        ProbesById = probes.GroupBy(p => p.Id).ToDictionary(g => g.Key, g => g.First());
    }

    public long Revision { get; }
    public BadgesMetadata Metadata { get; }
    public IReadOnlyList<Badge> Badges { get; }
    public IReadOnlyList<Probe> Probes { get; }
    public IReadOnlyDictionary<int, CatalogImages> AwardImages { get; }
    public IReadOnlyDictionary<string, Badge> BadgesById { get; }
    public IReadOnlyDictionary<string, Probe> ProbesById { get; }
}

/// <summary>
/// The catalogue in memory. <see cref="CatalogSyncBackgroundService"/> swaps in each new fetch;
/// readers always see one whole version.
/// </summary>
internal sealed class CatalogStore : IBadgesCatalog, IProbesCatalog
{
    private CatalogState _state = CatalogState.Empty;

    public CatalogState State => Volatile.Read(ref _state);

    public bool IsLoaded => State.Badges.Count > 0 || State.Probes.Count > 0;

    public void Replace(CatalogState state) => Volatile.Write(ref _state, state);

    public BadgesMetadata Metadata => State.Metadata;

    IReadOnlyList<Badge> IBadgesCatalog.GetAll() => State.Badges;

    public Badge? GetById(string id) => State.BadgesById.GetValueOrDefault(id);

    IReadOnlyList<Probe> IProbesCatalog.GetAll() => State.Probes;

    Probe? IProbesCatalog.GetById(string id) => State.ProbesById.GetValueOrDefault(id);
}
