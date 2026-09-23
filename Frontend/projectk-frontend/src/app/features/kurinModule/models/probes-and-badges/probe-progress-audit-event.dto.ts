import { ProbeProgressStatus } from '../enums/probe-progress-status.enum';

export interface ProbeProgressAuditEventDto {
  probeProgressAuditEventKey: string;
  fromStatus: ProbeProgressStatus | null;
  toStatus: ProbeProgressStatus;
  action: string;
  actorUserKey: string | null;
  actorName: string | null;
  actorRole: string;
  occurredAtUtc: string;
  note: string | null;
}