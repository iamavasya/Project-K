import { KurinBranch } from '../../features/kurinModule/models/enums/kurin-branch.enum';
import { compareKurinNumbers, kurinBranchesName, kurinBranchTags, kurinLabel } from './kurin-label.function';

describe('kurinLabel', () => {
  it('names a numbered kurin by its number', () => {
    expect(kurinLabel(47)).toBe('к. ч. 47');
  });

  it('names a підготовчий курінь without a number', () => {
    expect(kurinLabel(null)).toBe('підг. курінь');
  });

  it('names a senior kurin by its гілка', () => {
    expect(kurinLabel(5, KurinBranch.USP)).toBe('5-й курінь УСП');
    expect(kurinLabel(47, KurinBranch.UPYu)).toBe('к. ч. 47');
  });

  it('sorts підготовчі курені after the numbered ones', () => {
    expect([null, 12, 3].sort(compareKurinNumbers)).toEqual([3, 12, null]);
  });
});

describe('kurinBranchesName', () => {
  it('names one senior kurin in both of its гілки', () => {
    expect(kurinBranchesName([
      { branch: KurinBranch.USP, number: 5 },
      { branch: KurinBranch.UPS, number: 20 }
    ])).toBe('5-й курінь УСП · 20-й курінь УПС');
    expect(kurinBranchTags([{ branch: KurinBranch.USP, number: 5 }, { branch: KurinBranch.UPS, number: 20 }])).toBe('УСП · УПС');
  });

  it('keeps a УПЮ kurin as it always read', () => {
    expect(kurinBranchesName([{ branch: KurinBranch.UPYu, number: 47 }])).toBe('Курінь ч. 47');
    expect(kurinBranchesName(undefined, 12)).toBe('Курінь ч. 12');
  });
});
