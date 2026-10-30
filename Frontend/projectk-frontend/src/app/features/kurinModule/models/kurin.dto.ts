import { KurinBranch, KurinNumbering } from './enums/kurin-branch.enum';

export interface KurinDto {
  kurinKey: string;
  /** Null for a підготовчий курінь, which has no number yet. */
  number: number | null;
  isPreparatory?: boolean;
  /** The край the kurin belongs to; its number is unique within that край only. */
  kraiCode?: string;
  branch?: KurinBranch;
  /** Every гілка with the kurin's number there, main one first. */
  branches?: KurinNumbering[];
  stanytsia?: string | null;
  regionOrCountry?: string | null;
  namedAfter?: string | null;
  description?: string | null;
  currentUserCount?: number;
  profileVerificationEnabled?: boolean;
}
