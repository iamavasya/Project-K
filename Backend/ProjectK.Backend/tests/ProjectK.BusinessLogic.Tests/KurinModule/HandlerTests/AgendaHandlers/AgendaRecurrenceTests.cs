using FluentAssertions;
using ProjectK.BusinessLogic.Modules.KurinModule.Services;
using ProjectK.Common.Entities.KurinModule.Agenda;
using ProjectK.Common.Models.Enums;
using Xunit;

namespace ProjectK.BusinessLogic.Tests.KurinModule.HandlerTests.AgendaHandlers
{
    public class AgendaRecurrenceTests
    {
        private static AgendaItem Item(DateTime start, DateTime? end = null) => new()
        {
            KurinKey = Guid.NewGuid(),
            StartUtc = start,
            EndUtc = end
        };

        private static DateTime Utc(int y, int m, int d, int h = 9) => new(y, m, d, h, 0, 0, DateTimeKind.Utc);

        [Fact]
        public void NoRecurrence_YieldsSingleOccurrence()
        {
            var item = Item(Utc(2026, 8, 3));
            item.RecurrenceFrequency = RecurrenceFrequency.None;

            var occ = AgendaRecurrence.Expand(item, Utc(2026, 8, 1), Utc(2026, 8, 31)).ToList();

            occ.Should().ContainSingle().Which.StartUtc.Should().Be(Utc(2026, 8, 3));
        }

        [Fact]
        public void Weekly_EveryWeek_YieldsOnePerWeekInWindow()
        {
            // Monday 3 Aug 2026, weekly, default weekday (its own).
            var item = Item(Utc(2026, 8, 3));
            item.RecurrenceFrequency = RecurrenceFrequency.Weekly;
            item.RecurrenceInterval = 1;

            var occ = AgendaRecurrence.Expand(item, Utc(2026, 8, 1), Utc(2026, 8, 31)).ToList();

            occ.Select(o => o.StartUtc).Should().Equal(
                Utc(2026, 8, 3), Utc(2026, 8, 10), Utc(2026, 8, 17), Utc(2026, 8, 24), Utc(2026, 8, 31));
        }

        [Fact]
        public void Weekly_WithWeekdayMask_YieldsSelectedDays()
        {
            // Start Mon 3 Aug; mask = Tue+Thu (bit2 + bit4).
            var item = Item(Utc(2026, 8, 3));
            item.RecurrenceFrequency = RecurrenceFrequency.Weekly;
            item.RecurrenceByWeekday = (1 << 2) | (1 << 4); // Tuesday, Thursday

            var occ = AgendaRecurrence.Expand(item, Utc(2026, 8, 3), Utc(2026, 8, 14)).ToList();

            occ.Select(o => o.StartUtc).Should().Equal(
                Utc(2026, 8, 4), Utc(2026, 8, 6), Utc(2026, 8, 11), Utc(2026, 8, 13));
        }

        [Fact]
        public void Weekly_RespectsCountAcrossWholeSeries()
        {
            var item = Item(Utc(2026, 8, 3));
            item.RecurrenceFrequency = RecurrenceFrequency.Weekly;
            item.RecurrenceCount = 3;

            var occ = AgendaRecurrence.Expand(item, Utc(2026, 8, 1), Utc(2026, 12, 31)).ToList();

            occ.Select(o => o.StartUtc).Should().Equal(Utc(2026, 8, 3), Utc(2026, 8, 10), Utc(2026, 8, 17));
        }

        [Fact]
        public void Weekly_RespectsEndDate()
        {
            var item = Item(Utc(2026, 8, 3));
            item.RecurrenceFrequency = RecurrenceFrequency.Weekly;
            item.RecurrenceEndUtc = Utc(2026, 8, 17, 23);

            var occ = AgendaRecurrence.Expand(item, Utc(2026, 8, 1), Utc(2026, 9, 30)).ToList();

            occ.Should().HaveCount(3);
            occ.Last().StartUtc.Should().Be(Utc(2026, 8, 17));
        }

        [Fact]
        public void Monthly_KeepsDayOfMonth_AndCarriesDuration()
        {
            var item = Item(Utc(2026, 1, 15, 18), Utc(2026, 1, 15, 20));
            item.RecurrenceFrequency = RecurrenceFrequency.Monthly;

            var occ = AgendaRecurrence.Expand(item, Utc(2026, 1, 1), Utc(2026, 4, 30)).ToList();

            occ.Select(o => o.StartUtc).Should().Equal(
                Utc(2026, 1, 15, 18), Utc(2026, 2, 15, 18), Utc(2026, 3, 15, 18), Utc(2026, 4, 15, 18));
            occ.First().EndUtc.Should().Be(Utc(2026, 1, 15, 20));
        }

        [Fact]
        public void Monthly_SkipsMonthsWithoutThatDay()
        {
            // 31st: Feb/Apr/Jun... have no 31st and are skipped, not shifted.
            var item = Item(Utc(2026, 1, 31, 9));
            item.RecurrenceFrequency = RecurrenceFrequency.Monthly;

            var occ = AgendaRecurrence.Expand(item, Utc(2026, 1, 1), Utc(2026, 5, 31)).ToList();

            occ.Select(o => o.StartUtc).Should().Equal(Utc(2026, 1, 31, 9), Utc(2026, 3, 31, 9), Utc(2026, 5, 31, 9));
        }

        [Fact]
        public void Yearly_KeepsMonthAndDay()
        {
            var item = Item(Utc(2024, 6, 12));
            item.RecurrenceFrequency = RecurrenceFrequency.Yearly;

            var occ = AgendaRecurrence.Expand(item, Utc(2026, 1, 1), Utc(2027, 12, 31)).ToList();

            occ.Select(o => o.StartUtc).Should().Equal(Utc(2026, 6, 12), Utc(2027, 6, 12));
        }
    }
}
