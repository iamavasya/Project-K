import { BadgeProgressStatus } from '../enums/badge-progress-status.enum';

export interface BadgeProgressAuditEventDto {
  badgeProgressAuditEventKey: string;
  fromStatus: BadgeProgressStatus | null;
  toStatus: BadgeProgressStatus;
  action: string;
  actorUserKey: string | null;
  actorName: string | null;
  actorRole: string;
  occurredAtUtc: string;
  note: string | null;
}