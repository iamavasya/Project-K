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
/// </summary>
public static class AgendaRecurrence
{
    // Guards an open-ended weekly series with a distant window from spinning forever.
    private const int SafetyCap = 1000;

    public static IEnumerable<AgendaOccurrence> Expand(AgendaItem item, DateTime windowFrom, DateTime windowTo)
    {
        if (!item.StartUtc.HasValue)
        {
            yield break;
        }

        var start = item.StartUtc.Value;
        var duration = (item.EndUtc ?? start) - start;
        var hasEnd = item.EndUtc.HasValue;

        if (item.RecurrenceFrequency == RecurrenceFrequency.None)
        {
            yield return new AgendaOccurrence(start, item.EndUtc);
            yield break;
        }

        var interval = Math.Max(1, item.RecurrenceInterval);
        var seriesEnd = item.RecurrenceEndUtc;
        var maxCount = item.RecurrenceCount;
        var produced = 0;

        foreach (var occStart in EnumerateStarts(item, start, interval, windowTo))
        {
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
