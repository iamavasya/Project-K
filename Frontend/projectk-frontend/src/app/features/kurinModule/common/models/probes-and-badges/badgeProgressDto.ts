import { BadgeProgressStatus } from '../enums/badge-progress-status.enum';
import { BadgeProgressAuditEventDto } from './badgeProgressAuditEventDto';

export interface BadgeProgressDto {
  badgeProgressKey: string;
  memberKey: string;
  kurinKey: string;
  badgeId: string;
  status: BadgeProgressStatus | keyof typeof BadgeProgressStatus;
  submittedAtUtc: string | null;
  reviewedAtUtc: string | null;
  reviewedByUserKey: string | null;
  reviewedByName: string | null;
  reviewedByRole: string | null;
  reviewNote: string | null;
  auditTrail: BadgeProgressAuditEventDto[];
  memberFirstName?: string | null;
  memberLastName?: string | null;
  memberPhotoUrl?: string | null;
}