import { KurinBranch } from './enums/kurin-branch.enum';

export interface KurinDto {
  kurinKey: string;
  number: number;
  branch?: KurinBranch;
  stanytsia?: string | null;
  regionOrCountry?: string | null;
  namedAfter?: string | null;
  description?: string | null;
  currentUserCount?: number;
  profileVerificationEnabled?: boolean;
}
