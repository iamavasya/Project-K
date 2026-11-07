import { UserDto } from '../models/user.dto';
import { compareKurinsByBranch, KurinNaming, kurinGroupTitle } from '../../../shared/functions/kurin-label.function';

/** A user row with the kurin group it sits under. */
export interface UserRow extends UserDto {
  /** The group's key: the kurin's, or «none» for an account in no kurin. */
  group: string;
  /** The group's place in the list; the table groups and sorts by it. */
  groupOrder: number;
  groupTitle: string;
}

export const NO_KURIN_GROUP = 'none';

function naming(user: UserDto): KurinNaming {
  return { number: user.kurinNumber, branch: user.kurinBranch, branches: user.kurinBranches, namedAfter: user.kurinNamedAfter };
}

/**
 * The users grouped by the kurin they stand in, by its key and not its number: 5-й курінь УПЮ and
 * 5-й курінь УСП are two groups. УПЮ kurins first, senior ones after, accounts in no kurin last.
 */
export function usersByKurin(users: readonly UserDto[]): UserRow[] {
  const kurins = new Map<string, UserDto>();
  for (const user of users) {
    if (user.kurinKey && !kurins.has(user.kurinKey)) {
      kurins.set(user.kurinKey, user);
    }
  }

  const ordered = [...kurins.entries()]
    .sort(([keyA, a], [keyB, b]) => compareKurinsByBranch(naming(a), naming(b)) || keyA.localeCompare(keyB));
  const order = new Map(ordered.map(([key], index) => [key, index]));
  const titles = new Map(ordered.map(([key, user]) => [key, kurinGroupTitle(naming(user))]));

  return users
    .map(user => user.kurinKey
      ? { ...user, group: user.kurinKey, groupOrder: order.get(user.kurinKey)!, groupTitle: titles.get(user.kurinKey)! }
      : { ...user, group: NO_KURIN_GROUP, groupOrder: ordered.length, groupTitle: 'Без куреня' })
    .sort((a, b) => a.groupOrder - b.groupOrder);
}
