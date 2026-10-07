import { periodParams, periodQueryFromParams, periodQueryOf, periodValue, points, score } from './score-format.function';

describe('score format', () => {
  it('пише бали зі знаком і справжнім мінусом', () => {
    expect(points(3)).toBe('+3');
    expect(points(-1)).toBe('−1');
    expect(points(0)).toBe('0');
  });

  it('пише бал гуртка без зайвих знаків після коми', () => {
    expect(score(10)).toBe('10');
    expect(score(10.5)).toBe('10,5');
    expect(score(-2.25)).toBe('−2,25');
  });

  // The period travels three ways — select value, query string, request — and must not change on the way.
  it('період іде через селект і рядок запиту без втрат', () => {
    const year = { kind: 'Year' as const, year: 2026, stageKey: null, label: '26–27', from: '2026-10-01', to: '2027-09-30' };
    const stage = { kind: 'Stage' as const, year: null, stageKey: 'abc', label: 'Осінь', from: '2026-09-01', to: '2026-11-30' };

    expect(periodQueryOf(periodValue(year))).toEqual({ year: 2026 });
    expect(periodQueryOf(periodValue(stage))).toEqual({ stageKey: 'abc' });
    expect(periodQueryFromParams(periodParams({ year: 2026 }))).toEqual({ year: 2026 });
    expect(periodQueryFromParams(periodParams({ stageKey: 'abc' }))).toEqual({ stageKey: 'abc' });
    expect(periodQueryFromParams({})).toEqual({});
  });
});
