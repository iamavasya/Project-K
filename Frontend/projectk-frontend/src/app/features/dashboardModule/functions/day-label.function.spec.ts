import { dayLabel, timeLabel } from './day-label.function';

describe('day label', () => {
  const today = new Date(2026, 9, 7, 15, 30);

  it('says today and tomorrow by the calendar day, not by 24 hours', () => {
    expect(dayLabel(new Date(2026, 9, 7, 9), today)).toBe('сьогодні');
    expect(dayLabel(new Date(2026, 9, 8, 1), today)).toBe('завтра');
    expect(dayLabel(new Date(2026, 9, 10, 10), today)).toBe('сб, 10 жов');
  });

  it('writes the time span and nothing for an all-day item', () => {
    expect(timeLabel(new Date(2026, 9, 7, 10, 0), new Date(2026, 9, 7, 12, 0), false)).toBe('10:00–12:00');
    expect(timeLabel(new Date(2026, 9, 7, 10, 5), null, false)).toBe('10:05');
    expect(timeLabel(new Date(2026, 9, 7), null, true)).toBe('');
  });
});
