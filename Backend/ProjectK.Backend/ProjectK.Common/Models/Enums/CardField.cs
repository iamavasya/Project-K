using System.Text.Json.Serialization;

namespace ProjectK.Common.Models.Enums;

/// <summary>
/// What a shared member card may show besides the name. Stored as flags. Contacts, address, date of
/// birth, school, parents and the вкладка are not here on purpose: no card ever shows them.
/// </summary>
[Flags]
[JsonConverter(typeof(JsonNumberEnumConverter<CardField>))]
public enum CardField
{
    None = 0,
    Photo = 1,

    /// <summary>The kurins the person stands in, with the гурток.</summary>
    Kurin = 2,

    /// <summary>The ступінь.</summary>
    Level = 4,

    /// <summary>Confirmed відзначення.</summary>
    Awards = 8,

    /// <summary>Confirmed вмілості.</summary>
    Badges = 16,

    /// <summary>Verified проби.</summary>
    Probes = 32,

    All = Photo | Kurin | Level | Awards | Badges | Probes
}
