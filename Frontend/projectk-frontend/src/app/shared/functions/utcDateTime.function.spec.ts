import { normalizeUtcDateTimeString, parseUtcDateTime } from './utcDateTime.function';

describe('utcDateTime functions', () => {
  it('treats date-time strings without timezone as UTC', () => {
    const parsed = parseUtcDateTime('2026-07-04T20:53:00');

    expect(parsed?.toISOString()).toBe('2026-07-04T20:53:00.000Z');
  });

  it('keeps explicit UTC and offset timestamps unchanged', () => {
    expect(normalizeUtcDateTimeString('2026-07-04T20:53:00Z')).toBe('2026-07-04T20:53:00Z');
    expect(normalizeUtcDateTimeString('2026-07-04T23:53:00+03:00')).toBe('2026-07-04T23:53:00+03:00');
  });

  it('does not rewrite date-only values', () => {
    expect(normalizeUtcDateTimeString('2026-07-04')).toBe('2026-07-04');
  });
});
