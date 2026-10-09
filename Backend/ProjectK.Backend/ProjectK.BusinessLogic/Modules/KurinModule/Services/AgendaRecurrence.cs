using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Models.Enums;

namespace ProjectK.BusinessLogic.Modules.KurinModule.Services;

/// <summary>One materialised occurrence of a (possibly recurring) item.</summary>
public readonly record struct AgendaOccurrence(DateTime StartUtc, DateTime? EndUtc);

/// <summary>
/// Expands a recurrence rule into concrete occurrences inside a query window. The rule is deliberately
/// simple (frequency + interval + weekly weekday mask + end date/count) and expansion happens on the
/// server, so the calendar never needs an rrule client. Occurrences are produced in chronological order
/// from the series start so the <c>count</c> limit is honoured across the whole series, not just the window.
/// <para>
/// The series repeats by the clock on the wall, not by the UTC instant: сходини at 18:30 stay at 18:30
/// after the clocks go back. Stepping the stored UTC start by whole days moved every occurrence after
/// the last Sunday of October an hour earlier (found live, 1.1.4). Occurrences are therefore stepped in
/// <see cref="DefaultZone"/> and converted back; a time the zone skips (the spring gap) lands an hour later.
/// </para>
/// </summary>
public static class AgendaRecurrence
{
    // Guards an open-ended weekly series with a distant window from spinning forever.
    private const int SafetyCap = 1000;

    /// <summary>
    /// The zone a kurin's week is reckoned in. One for the instance until a kurin carries its own:
    /// every kurin so far keeps the EU clock-change dates, so the wall-clock time holds for all of them.
    /// Set once at startup from <c>Agenda:TimeZone</c>; defaults to Kyiv.
    /// </summary>
    public static TimeZoneInfo DefaultZone { get; set; } = ResolveZone("Europe/Kyiv");

    /// <summary>The zone behind an IANA id, or UTC when it is unknown — a bad setting must not take the calendar down.</summary>
    public static TimeZoneInfo ResolveZone(string? timeZoneId)
        => !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone)
            ? zone
            : TimeZoneInfo.Utc;

    public static IEnumerable<AgendaOccurrence> Expand(AgendaItem item, DateTime windowFrom, DateTime windowTo)
        => Expand(item, windowFrom, windowTo, DefaultZone);

    public static IEnumerable<AgendaOccurrence> Expand(AgendaItem item, DateTime windowFrom, DateTime windowTo, TimeZoneInfo zone)
    {
        if (!item.StartUtc.HasValue)
        {
            yield break;
        }

        var start = DateTime.SpecifyKind(item.StartUtc.Value, DateTimeKind.Utc);
        var duration = (item.EndUtc ?? start) - start;
        var hasEnd = item.EndUtc.HasValue;

        if (item.RecurrenceFrequency == RecurrenceFrequency.None)
        {
            yield return new AgendaOccurrence(item.StartUtc.Value, item.EndUtc);
            yield break;
        }

        var interval = Math.Max(1, item.RecurrenceInterval);
        var seriesEnd = item.RecurrenceEndUtc;
        var maxCount = item.RecurrenceCount;
        var produced = 0;

        // Stepped on the wall clock of the zone, each step taken back to the instant it names.
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(start, zone);
        var localWindowTo = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(windowTo, DateTimeKind.Utc), zone);

        foreach (var localOcc in EnumerateStarts(item, localStart, interval, localWindowTo))
        {
            var occStart = ToUtc(localOcc, zone);
            if (produced >= SafetyCap)
            {
                yield break;
            }

            if (maxCount.HasValue && produced >= maxCount.Value)
            {
                yield break;
            }

            if (seriesEnd.HasValue && occStart > seriesEnd.Value)
            {
                yield break;
            }

            produced++;

            var occEnd = occStart + duration;
            if (occEnd >= windowFrom && occStart <= windowTo)
            {
                yield return new AgendaOccurrence(occStart, hasEnd ? occEnd : null);
            }
        }
    }

    /// <summary>A wall-clock moment as the instant it names; a moment the zone skips is taken an hour later.</summary>
    private static DateTime ToUtc(DateTime local, TimeZoneInfo zone)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        if (zone.IsInvalidTime(unspecified))
        {
            unspecified = unspecified.AddHours(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(unspecified, zone);
    }

    private static IEnumerable<DateTime> EnumerateStarts(AgendaItem item, DateTime start, int interval, DateTime windowTo)
    {
        return item.RecurrenceFrequency switch
        {
            RecurrenceFrequency.Weekly => WeeklyStarts(item, start, interval, windowTo),
            RecurrenceFrequency.Monthly => SteppedStarts(start, windowTo, k => AddMonthsExact(start, k * interval)),
            RecurrenceFrequency.Yearly => SteppedStarts(start, windowTo, k => AddYearsExact(start, k * interval)),
            _ => Enumerable.Empty<DateTime>()
        };
    }

    private static IEnumerable<DateTime> WeeklyStarts(AgendaItem item, DateTime start, int interval, DateTime windowTo)
    {
        var mask = item.RecurrenceByWeekday == 0 ? 1 << (int)start.DayOfWeek : item.RecurrenceByWeekday;
        var timeOfDay = start.TimeOfDay;
        var weekStart = DateTime.SpecifyKind(start.Date.AddDays(-(int)start.DayOfWeek), start.Kind);

        for (var week = 0; ; week += interval)
        {
            var baseDate = weekStart.AddDays(week * 7);
            if (baseDate > windowTo.Date.AddDays(7))
            {
                yield break;
            }

            for (var day = 0; day < 7; day++)
            {
                if ((mask & (1 << day)) == 0)
                {
                    continue;
                }

                var occ = baseDate.AddDays(day) + timeOfDay;
                if (occ < start)
                {
                    continue;
                }

                yield return occ;
            }
        }
    }

    private static IEnumerable<DateTime> SteppedStarts(DateTime start, DateTime windowTo, Func<int, DateTime?> at)
    {
        for (var k = 0; ; k++)
        {
            var occ = at(k);
            if (occ is null)
            {
                if (k > SafetyCap)
                {
                    yield break;
                }
                continue;
            }

            if (occ.Value > windowTo)
            {
                yield break;
            }

            yield return occ.Value;
        }
    }

    /// <summary>Adds whole months keeping the day-of-month; returns null when the target month lacks that day.</summary>
    private static DateTime? AddMonthsExact(DateTime start, int months)
    {
        var monthIndex = start.Month - 1 + months;
        var year = start.Year + monthIndex / 12;
        var month = monthIndex % 12 + 1;
        if (start.Day > DateTime.DaysInMonth(year, month))
        {
            return null;
        }

        return new DateTime(year, month, start.Day, start.Hour, start.Minute, start.Second, start.Kind);
    }

    /// <summary>Adds whole years keeping month/day; returns null for 29 Feb in a non-leap target year.</summary>
    private static DateTime? AddYearsExact(DateTime start, int years)
    {
        var year = start.Year + years;
        if (start.Day > DateTime.DaysInMonth(year, start.Month))
        {
            return null;
        }

        return new DateTime(year, start.Month, start.Day, start.Hour, start.Minute, start.Second, start.Kind);
    }
}
