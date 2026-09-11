import { KurinBranch } from './enums/kurin-branch.enum';
import { MembershipKind } from './enums/membership-kind.enum';

/**
 * Один відтинок належності: курінь, гурток і час, коли це тривало. Курінь названий, а не лише
 * ключований, — тека приходить з іншого модуля й донести назву мусить вона сама.
 */
export interface MembershipDto {
  membershipKey: string;
  kurinKey: string;
  kurinNumber: number;
  branch: KurinBranch;
  kurinNamedAfter?: string | null;
  groupKey?: string | null;
  groupName?: string | null;
  kind: MembershipKind;
  joinedAtUtc: string;
  leftAtUtc?: string | null;
  isCurrent: boolean;
}
