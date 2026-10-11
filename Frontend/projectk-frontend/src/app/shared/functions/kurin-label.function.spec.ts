import { compareKurinNumbers, kurinLabel } from './kurin-label.function';

describe('kurinLabel', () => {
  it('names a numbered kurin by its number', () => {
    expect(kurinLabel(47)).toBe('к. ч. 47');
  });

  it('names a підготовчий курінь without a number', () => {
    expect(kurinLabel(null)).toBe('підг. курінь');
  });

  it('sorts підготовчі курені after the numbered ones', () => {
    expect([null, 12, 3].sort(compareKurinNumbers)).toEqual([3, 12, null]);
  });
});
