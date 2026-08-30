import { LeadershipRole } from '../models/enums/leadership-role.enum';
import { LeadershipHistoryDto } from '../models/requests/leadership/leadershipDto';
import { MemberLookupDto } from '../models/requests/member/memberLookupDto';
import { ROLE_DISPLAY_NAMES } from '../models/roleDisplayName';

/** The severities the tag component understands. `warning` is not one of them. */
export type RoleSeverity = 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast';

/**
 * The colour an office is shown in.
 *
 * The member list and the leadership panel each had their own version of this, and they disagreed:
 * Зв'язковий was red in one and blue in the other, Скарбник green in one and blue in the other, so
 * the same office changed colour depending on the screen. This is the union of both, which keeps
 * every distinction either of them made.
 */
export function leadershipRoleSeverity(history: LeadershipHistoryDto): RoleSeverity {
  if (history.endDate) {
    return 'secondary';
  }

  switch (history.role as LeadershipRole) {
    case LeadershipRole.Kurinnuy:
    case LeadershipRole.Zvyazkovyi:
    case LeadershipRole.Hurtkoviy:
      return 'danger';
    case LeadershipRole.Suddya:
      return 'warn';
    case LeadershipRole.Skarbnyk:
      return 'success';
    default:
      return 'info';
  }
}

/** The office's Ukrainian name, falling back to the raw value for anything unmapped. */
export function leadershipRoleDisplayName(role: LeadershipRole | string): string {
  return ROLE_DISPLAY_NAMES[role as LeadershipRole] || String(role);
}

/** "Прізвище Ім'я По батькові", the way every list renders a member. */
export function memberDisplayName(member: MemberLookupDto): string {
  return `${member.lastName} ${member.firstName}${member.middleName ? ` ${member.middleName}` : ''}`;
}
