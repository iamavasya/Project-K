import { KurinBranch } from './enums/kurin-branch.enum';

export interface KurinDto {
  kurinKey: string;
  /** Null for a підготовчий курінь, which has no number yet. */
  number: number | null;
  isPreparatory?: boolean;
  branch?: KurinBranch;
  stanytsia?: string | null;
  regionOrCountry?: string | null;
  namedAfter?: string | null;
  description?: string | null;
  currentUserCount?: number;
  profileVerificationEnabled?: boolean;
}
