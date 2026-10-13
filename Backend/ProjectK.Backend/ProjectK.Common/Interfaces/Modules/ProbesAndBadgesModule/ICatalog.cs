using ProjectK.Common.Models.Catalog;

namespace ProjectK.Common.Interfaces.Modules.ProbesAndBadgesModule;

/// <summary>The вмілості from the catalogue service, as last fetched.</summary>
public interface IBadgesCatalog
{
    BadgesMetadata Metadata { get; }

    IReadOnlyList<Badge> GetAll();

    Badge? GetById(string id);
}

/// <summary>The probes from the catalogue service, as last fetched.</summary>
public interface IProbesCatalog
{
    IReadOnlyList<Probe> GetAll();

    Probe? GetById(string id);
}

/// <summary>Pictures of the УПЮ відзначення, by level.</summary>
public interface IAwardImagesCatalog
{
    Task<CatalogImage?> GetAwardImageAsync(int level, bool colored, CancellationToken cancellationToken = default);
}
