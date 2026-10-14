namespace ProjectK.Common.Models.KraiProfiles;

/// <summary>
/// A край's profile as it is stored: one JSON document per revision. Every value is optional; a
/// value left out is taken from code, so an older document still reads after Lilejka grows a new
/// setting. Adding a setting means a nullable field here, its default in
/// <c>KraiProfileProvider</c> and its place in <see cref="KraiProfile"/>.
/// </summary>
public sealed record KraiProfileDocument
{
    public const int CurrentSchemaVersion = 1;

    public KraiCatalogDocument? Catalog { get; init; }
    public KraiLocaleDocument? Locale { get; init; }
}

public sealed record KraiCatalogDocument
{
    /// <summary>Which краї's catalogue entries the kurins see, by the catalogue's own krai mark.</summary>
    public IReadOnlyList<string>? BadgeSources { get; init; }
    public IReadOnlyList<string>? ProbeSources { get; init; }

    /// <summary>Single entries the край leaves out of what its sources give.</summary>
    public IReadOnlyList<string>? HiddenBadges { get; init; }
    public IReadOnlyList<string>? HiddenProbes { get; init; }
}

public sealed record KraiLocaleDocument
{
    /// <summary>ISO 4217 code, such as UAH or CZK.</summary>
    public string? Currency { get; init; }

    /// <summary>IANA zone, such as Europe/Prague.</summary>
    public string? TimeZone { get; init; }

    /// <summary>How numbers and dates are written, such as uk-UA.</summary>
    public string? NumberLocale { get; init; }
}

/// <summary>A край's profile with every value filled in: what the rest of Lilejka reads.</summary>
public sealed record KraiProfile(
    string KraiCode,
    string KraiName,
    int Revision,
    KraiCatalogSettings Catalog,
    KraiLocaleSettings Locale);

public sealed record KraiCatalogSettings(
    IReadOnlyList<string> BadgeSources,
    IReadOnlyList<string> ProbeSources,
    IReadOnlyList<string> HiddenBadges,
    IReadOnlyList<string> HiddenProbes);

public sealed record KraiLocaleSettings(
    string Currency,
    string TimeZone,
    string NumberLocale);
