import { MemberWarningLevel } from './enums/member-warning-level.enum';

export interface MemberWarningDto {
  memberWarningKey: string;
  memberKey: string;
  level: MemberWarningLevel;
  issuedAtUtc: string;
  expiresAtUtc: string;
  issuedByUserKey: string;
  revokedByUserKey?: string | null;
  revokedAtUtc?: string | null;
}
