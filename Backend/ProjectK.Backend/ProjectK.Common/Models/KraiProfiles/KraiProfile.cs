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
    public KraiCardFieldsDocument? CardFields { get; init; }
}

public sealed record KraiCatalogDocument
{
    /// <summary>Which краї's вмілості the kurins see, by the catalogue's own krai mark.</summary>
    /// <remarks>Проби are not set here: the three юнацькі проби are the same across Plast.</remarks>
    public IReadOnlyList<string>? BadgeSources { get; init; }

    /// <summary>Single вмілості the край leaves out of what its sources give.</summary>
    public IReadOnlyList<string>? HiddenBadges { get; init; }
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

public sealed record KraiCardFieldsDocument
{
    /// <summary>
    /// How the card treats each of its own fields that a край may change, by name
    /// (<see cref="KraiCardFields.Configurable"/>): one of <see cref="KraiCardFields.Required"/>,
    /// <see cref="KraiCardFields.Optional"/> or <see cref="KraiCardFields.Hidden"/>.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Fields { get; init; }

    /// <summary>Fields of the край's own that the card does not have, such as a visa type.</summary>
    public IReadOnlyList<KraiCardExtraField>? Extra { get; init; }
}

/// <summary>A field a край adds to the card. Its value is kept on the member under <see cref="Key"/>.</summary>
public sealed record KraiCardExtraField
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;

    /// <summary><see cref="KraiCardFields.Text"/> or <see cref="KraiCardFields.Date"/>.</summary>
    public string Type { get; init; } = KraiCardFields.Text;

    public bool Required { get; init; }
}

public static class KraiCardFields
{
    public const string Required = "required";
    public const string Optional = "optional";
    public const string Hidden = "hidden";

    public const string Text = "text";
    public const string Date = "date";

    public const string MiddleName = "middleName";
    public const string PhoneNumber = "phoneNumber";

    /// <summary>
    /// The card's fields a край may make optional or hide, each with how it works without a
    /// profile. Name, email and date of birth are not here: an account and the age rules need them.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Configurable = new Dictionary<string, string>
    {
        [MiddleName] = Required,
        [PhoneNumber] = Required
    };

    /// <summary>How the card names its configurable fields to people.</summary>
    public static readonly IReadOnlyDictionary<string, string> Labels = new Dictionary<string, string>
    {
        [MiddleName] = "По батькові",
        [PhoneNumber] = "Номер телефону"
    };

    public static readonly IReadOnlyList<string> Modes = [Required, Optional, Hidden];
    public static readonly IReadOnlyList<string> Types = [Text, Date];
}

/// <summary>A край's profile with every value filled in: what the rest of Lilejka reads.</summary>
public sealed record KraiProfile(
    string KraiCode,
    string KraiName,
    int Revision,
    KraiCatalogSettings Catalog,
    KraiLocaleSettings Locale,
    KraiCardFieldsSettings CardFields);

public sealed record KraiCatalogSettings(
    IReadOnlyList<string> BadgeSources,
    IReadOnlyList<string> HiddenBadges);

public sealed record KraiLocaleSettings(
    string Currency,
    string TimeZone,
    string NumberLocale);

/// <summary>Every configurable field with its mode, and the край's own fields.</summary>
public sealed record KraiCardFieldsSettings(
    IReadOnlyDictionary<string, string> Fields,
    IReadOnlyList<KraiCardExtraField> Extra)
{
    /// <summary>The card as it is without a profile.</summary>
    public static readonly KraiCardFieldsSettings Default = new(KraiCardFields.Configurable, []);

    public string ModeOf(string field)
    {
        return Fields.TryGetValue(field, out var mode) ? mode : KraiCardFields.Configurable.GetValueOrDefault(field, KraiCardFields.Required);
    }

    /// <summary>
    /// The fields a kurin may leave off its card, with their names: the край's optional ones and
    /// its own fields that are not required. What the край requires a kurin cannot drop.
    /// </summary>
    public IReadOnlyList<(string Key, string Label)> Hideable()
    {
        return Fields
            .Where(field => field.Value == KraiCardFields.Optional)
            .Select(field => (field.Key, KraiCardFields.Labels.GetValueOrDefault(field.Key, field.Key)))
            .Concat(Extra.Where(field => !field.Required).Select(field => (field.Key, field.Label)))
            .ToList();
    }

    /// <summary>The card in a kurin that hides <paramref name="hidden"/>; a key it may not hide is ignored.</summary>
    public KraiCardFieldsSettings HidingFor(IReadOnlyCollection<string> hidden)
    {
        if (hidden.Count == 0)
        {
            return this;
        }

        var hideable = Hideable().Select(field => field.Key).Where(hidden.Contains).ToHashSet(StringComparer.Ordinal);
        return new KraiCardFieldsSettings(
            Fields.ToDictionary(field => field.Key, field => hideable.Contains(field.Key) ? KraiCardFields.Hidden : field.Value),
            Extra.Where(field => !hideable.Contains(field.Key)).ToList());
    }
}
