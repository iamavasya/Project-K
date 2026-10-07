import { KurinBranch } from '../models/enums/kurin-branch.enum';
import { personalBranch } from '../models/enums/plast-ladder';
import { PlastLevel } from '../models/enums/plast-level.enum';
import { MemberDto } from '../models/member.dto';
import { MembershipDto } from '../models/membership.dto';

/**
 * Гілка людини там, де ми її дивимось. Курінь бере членство саме тут — та сама людина може бути
 * юнаком в одному курені й старшим пластуном у другому, — а старший ступінь переважає курінь:
 * впорядник юнацького куреня сам належить до УСП чи УПС.
 */
export function memberBranchHere(member: MemberDto | null, memberships: readonly MembershipDto[]): KurinBranch {
  const here = memberships.find(m => m.isCurrent && m.kurinKey === member?.kurinKey)
    ?? memberships.find(m => m.isCurrent);
  const levels = (member?.plastLevelHistories ?? []).map(h => h.plastLevel);
  if (member?.latestPlastLevel) {
    levels.push(member.latestPlastLevel as PlastLevel);
  }

  return personalBranch(levels, here?.branch);
}
