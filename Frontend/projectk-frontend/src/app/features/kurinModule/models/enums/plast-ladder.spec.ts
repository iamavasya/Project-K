import { KurinBranch } from './kurin-branch.enum';
import { branchOfLevel, highestLevel, personalBranch } from './plast-ladder';
import { PlastLevel } from './plast-level.enum';

describe('plast-ladder', () => {
  it('ступінь належить своїй гілці', () => {
    expect(branchOfLevel(PlastLevel.Entry)).toBe(KurinBranch.UPYu);
    expect(branchOfLevel(PlastLevel.Prykhylnyk)).toBe(KurinBranch.UPYu);
    expect(branchOfLevel(PlastLevel.HetmanskiySkob)).toBe(KurinBranch.UPYu);
    expect(branchOfLevel(PlastLevel.Starshoplastun)).toBe(KurinBranch.USP);
    expect(branchOfLevel(PlastLevel.Senior)).toBe(KurinBranch.UPS);
    expect(branchOfLevel(PlastLevel.SeniorPratsi)).toBe(KurinBranch.UPS);
    expect(branchOfLevel(PlastLevel.SeniorDovirja)).toBe(KurinBranch.UPS);
    expect(branchOfLevel(PlastLevel.SeniorKerivnytstva)).toBe(KurinBranch.UPS);
  });

  it('найвищий ступінь — за драбиною, а не за значенням enum', () => {
    expect(highestLevel([PlastLevel.Prykhylnyk, PlastLevel.Uchasnyk])).toBe(PlastLevel.Uchasnyk);
    expect(highestLevel([PlastLevel.Starshoplastun, PlastLevel.Skob])).toBe(PlastLevel.Starshoplastun);
    expect(highestLevel([])).toBeNull();
  });

  describe('personalBranch', () => {
    it('старший пластун у курені УПЮ — УСП', () => {
      expect(personalBranch([PlastLevel.Skob, PlastLevel.Starshoplastun], KurinBranch.UPYu)).toBe(KurinBranch.USP);
    });

    it('юнак без ступеня в курені УСП — УСП', () => {
      expect(personalBranch([], KurinBranch.USP)).toBe(KurinBranch.USP);
    });

    it('юнак у курені УПЮ — УПЮ', () => {
      expect(personalBranch([PlastLevel.Rozviduvach], KurinBranch.UPYu)).toBe(KurinBranch.UPYu);
    });

    it('без ступеня й куреня — УПЮ', () => {
      expect(personalBranch([], null)).toBe(KurinBranch.UPYu);
    });

    it('сеніор у курені УСП — ступінь переважає', () => {
      expect(personalBranch([PlastLevel.SeniorPratsi], KurinBranch.USP)).toBe(KurinBranch.UPS);
    });
  });
});
