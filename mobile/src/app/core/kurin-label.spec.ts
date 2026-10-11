import { compareKurinNumbers, inKurin, kurinLabel, kurinName, kurinTitle } from './kurin-label';

describe('kurin labels', () => {
  it('names a numbered kurin by its number', () => {
    expect(kurinLabel(47)).toBe('к. ч. 47');
    expect(kurinName(47)).toBe('Курінь ч. 47');
    expect(kurinTitle(47)).toBe('47 курінь');
    expect(inKurin(47)).toBe('курені ч. 47');
  });

  it('names a підготовчий курінь without a number', () => {
    expect(kurinLabel(null)).toBe('підг. курінь');
    expect(kurinName(undefined)).toBe('Підготовчий курінь');
    expect(kurinTitle(null)).toBe('Підготовчий курінь');
    expect(inKurin(null)).toBe('підготовчому курені');
  });

  it('sorts підготовчі last', () => {
    expect([null, 12, 3].sort(compareKurinNumbers)).toEqual([3, 12, null]);
  });
});
