import { ProbeProgressStatus } from '../enums/probe-progress-status.enum';
import { ProbeProgressAuditEventDto } from './probeProgressAuditEventDto';

export interface ProbeProgressDto {
  probeProgressKey: string | null;
  memberKey: string;
  kurinKey: string;
  probeId: string;
  status: ProbeProgressStatus;
  completedAtUtc: string | null;
  completedByUserKey: string | null;
  completedByName: string | null;
  completedByRole: string | null;
  verifiedAtUtc: string | null;
  verifiedByUserKey: string | null;
  verifiedByName: string | null;
  verifiedByRole: string | null;
  auditTrail: ProbeProgressAuditEventDto[];
}