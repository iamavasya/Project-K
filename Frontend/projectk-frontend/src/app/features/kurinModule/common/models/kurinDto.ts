import { KurinBranch } from './enums/kurin-branch.enum';

export interface KurinDto {
  kurinKey: string;
  number: number;
  branch?: KurinBranch;
  managerEmail?: string;
  stanytsia?: string | null;
  regionOrCountry?: string | null;
  namedAfter?: string | null;
  description?: string | null;
  isZbtEnabled?: boolean;
  zbtUserCap?: number;
  currentUserCount?: number;
  profileVerificationEnabled?: boolean;
}
