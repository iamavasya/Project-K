import { KurinBranch } from './enums/kurin-branch.enum';
import { MembershipKind } from './enums/membership-kind.enum';

/** Курінь, у якому цей акаунт може діяти зараз. Приходить із `auth/kurin-scope/options`. */
export interface KurinScopeOption {
  kurinKey: string;
  /** Null for a підготовчий курінь. */
  kurinNumber: number | null;
  branch: KurinBranch;
  namedAfter?: string | null;
  kind: MembershipKind;
}
