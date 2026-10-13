import { fullName } from './kurin.labels';
import { MemberLookupDto } from './kurin.models';

/*
 * The rules of the web's people controls on a гурток (group-panel's «Додати учасника» and
 * «Впорядники», and member-card's «Гурток»), pure so the page and its sheets stay views. Its провід
 * is leadership-seats.ts.
 */

const EMPTY_GUID = '00000000-0000-0000-0000-000000000000';

// ── Впорядники (web group-panel saveMentorAssignments) ────────────────────────────────────────────

/** The user keys to assign and to revoke, from the ones there before and the ones ticked now. */
export function mentorChanges(before: string[], after: string[]): { assign: string[]; revoke: string[] } {
  const was = new Set(before);
  const now = new Set(after);
  return { assign: [...now].filter((key) => !was.has(key)), revoke: [...was].filter((key) => !now.has(key)) };
}

// ── Склад гуртка (web membership moveToGroup) ────────────────────────────────────────────────────

/** A real гурток key: list reads send Guid.Empty for someone in no гурток. */
export function placedIn(member: Pick<MemberLookupDto, 'groupKey'>): string | null {
  const key = member.groupKey ?? null;
  return key && key !== EMPTY_GUID ? key : null;
}

/**
 * Who may be added to this гурток from the kurin: everyone placed elsewhere or nowhere, those in no
 * гурток first (they are who usually waits for one), each part by surname.
 */
export function addableToGroup(kurinMembers: MemberLookupDto[], groupKey: string, inGroup: MemberLookupDto[] = []): MemberLookupDto[] {
  const here = new Set(inGroup.map((m) => m.memberKey));
  const byName = (a: MemberLookupDto, b: MemberLookupDto) => fullName(a).localeCompare(fullName(b), 'uk');
  const rest = kurinMembers.filter((m) => placedIn(m) !== groupKey && !here.has(m.memberKey));
  return [...rest.filter((m) => !placedIn(m)).sort(byName), ...rest.filter((m) => !!placedIn(m)).sort(byName)];
}

/** The line under a name in that picker: where they are now. */
export function placementLine(member: Pick<MemberLookupDto, 'groupKey' | 'groupName'>): string {
  return placedIn(member) ? `Зараз: ${member.groupName || 'інший гурток'}` : 'Без гуртка';
}
