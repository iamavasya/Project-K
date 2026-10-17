using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Interfaces.Modules.KraiModule;
using ProjectK.Common.Models.Dues;

namespace ProjectK.BusinessLogic.Modules.DuesModule.Services;

/// <summary>
/// What a kurin's вкладка takes from its край: the currency it is counted in, how its numbers are
/// written, and the clock its quarters turn on. A quarter turns at midnight in the край's zone, not
/// in UTC, so a kurin in Kyiv is not charged two hours late, nor one in Toronto five hours early.
/// </summary>
public sealed record DuesLocale(string Currency, string NumberLocale, TimeZoneInfo Zone)
{
    /// <summary>How Lilejka counted before краї had profiles.</summary>
    public static DuesLocale Default { get; } = new("UAH", "uk-UA", AgendaRecurrence.DefaultZone);

    /// <summary>The day it is in the край when the clock reads <paramref name="now"/>.</summary>
    public DateOnly Today(DateTimeOffset now) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, Zone).DateTime);

    /// <summary>The quarter it is in the край when the clock reads <paramref name="now"/>.</summary>
    public DuesQuarter QuarterOf(DateTimeOffset now) => DuesQuarter.Of(Today(now));

    /// <summary>The quarter a moment stored in UTC falls into in the край.</summary>
    public DuesQuarter QuarterOf(DateTime utc) => QuarterOf(new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc)));
}

public static class DuesLocales
{
    public static async Task<DuesLocale> DuesLocaleForKurinAsync(this IKraiProfileProvider profiles, Guid kurinKey, CancellationToken cancellationToken)
    {
        var profile = await profiles.GetForKurinAsync(kurinKey, cancellationToken);
        return profile is null
            ? DuesLocale.Default
            : new DuesLocale(profile.Locale.Currency, profile.Locale.NumberLocale, AgendaRecurrence.ResolveZone(profile.Locale.TimeZone));
    }
}
