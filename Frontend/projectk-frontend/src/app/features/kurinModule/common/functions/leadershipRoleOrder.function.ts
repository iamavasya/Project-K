import { LeadershipRole } from '../models/enums/leadership-role.enum';
import { LeadershipHistoryDto } from '../models/requests/leadership/leadershipDto';

export const LEADERSHIP_ROLE_ORDER: readonly LeadershipRole[] = [
  LeadershipRole.Zvyazkovyi,
  LeadershipRole.Kurinnuy,
  LeadershipRole.Hurtkoviy,
  LeadershipRole.Suddya,
  LeadershipRole.Pysar,
  LeadershipRole.Skarbnyk,
  LeadershipRole.Horunjiy,
  LeadershipRole.Gospodar,
  LeadershipRole.Hronikar,
  LeadershipRole.Instruktor,
  LeadershipRole.Vykhovnyk
];

const LEADERSHIP_ROLE_WEIGHTS = new Map<string, number>(
  LEADERSHIP_ROLE_ORDER.map((role, index) => [role, index])
);

export function getLeadershipRoleSortWeight(role?: string | null): number {
  return LEADERSHIP_ROLE_WEIGHTS.get(role ?? '') ?? Number.MAX_SAFE_INTEGER;
}

export function compareLeadershipHistoriesByDefault(
  left: LeadershipHistoryDto,
  right: LeadershipHistoryDto
): number {
  const leftActive = !left.endDate;
  const rightActive = !right.endDate;

  if (leftActive !== rightActive) {
    return leftActive ? -1 : 1;
  }

  const roleWeight = getLeadershipRoleSortWeight(left.role) - getLeadershipRoleSortWeight(right.role);
  if (roleWeight !== 0) {
    return roleWeight;
  }

  const name = getLeadershipTieBreaker(left).localeCompare(getLeadershipTieBreaker(right));
  if (name !== 0) {
    return name;
  }

  return getDateTime(right.startDate) - getDateTime(left.startDate);
}

function getLeadershipTieBreaker(history: LeadershipHistoryDto): string {
  const member = history.member;
  if (member) {
    return `${member.lastName ?? ''} ${member.firstName ?? ''} ${member.middleName ?? ''}`.trim().toLowerCase();
  }

  return (history.groupName ?? history.role ?? '').toLowerCase();
}

function getDateTime(value?: string | null): number {
  return value ? new Date(value).getTime() : 0;
}
