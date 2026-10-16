import { LeadershipRole } from '../models/enums/leadership-role.enum';
import { getLeadershipRoleSortWeight } from './leadership-role-order.function';
import { leadershipRoleDisplayName, leadershipRoleSeverityForRole, RoleSeverity } from './leadership-role-display.function';
import { parseOfficeRole } from './system-role.function';

export interface AccountRoleTag {
  label: string;
  severity: RoleSeverity;
}

export const GENERIC_MEMBER_TAG: AccountRoleTag = { label: 'Учасник', severity: 'info' };

/**
 * The office to show when an account holds several. Ordered by the same weights the member list
 * sorts by, so "most senior" means one thing across the app.
 */
export function mostSeniorOffice(systemRoles: string[]): LeadershipRole | null {
  return systemRoles
    .map(parseOfficeRole)
    .filter((office): office is NonNullable<ReturnType<typeof parseOfficeRole>> => office !== null)
    .map(office => office.role)
    .sort((left, right) => getLeadershipRoleSortWeight(left) - getLeadershipRoleSortWeight(right))[0] ?? null;
}

/**
 * How an account is labelled in the account switcher: administrator, else its most senior office,
 * else `fallback`. The sidebar passes the tier it reads from the signed-in account's permissions.
 */
export function accountRoleTag(isAdmin: boolean, systemRoles: string[], fallback: AccountRoleTag = GENERIC_MEMBER_TAG): AccountRoleTag {
  if (isAdmin) {
    return { label: 'Адміністратор', severity: 'danger' };
  }

  const office = mostSeniorOffice(systemRoles);
  return office
    ? { label: leadershipRoleDisplayName(office), severity: leadershipRoleSeverityForRole(office) }
    : fallback;
}

/** What the role tag needs to know about the signed-in account beyond its offices. */
export interface RoleTier {
  isAdmin(): boolean;
  canManageWholeKurin(): boolean;
  canLeadGroups(): boolean;
}

/**
 * The signed-in account's tag, as the sidebar footer and the account page show it: its office first,
 * else the tier its permissions give it ("Провід куреня", "Гуртковий провід"), else a member.
 */
export function signedInRoleTag(tier: RoleTier, systemRoles: string[]): AccountRoleTag {
  let fallback = GENERIC_MEMBER_TAG;
  if (tier.canManageWholeKurin()) {
    fallback = { label: 'Провід куреня', severity: 'warn' };
  } else if (tier.canLeadGroups()) {
    fallback = { label: 'Гуртковий провід', severity: 'success' };
  }

  return accountRoleTag(tier.isAdmin(), systemRoles, fallback);
}
