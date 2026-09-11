import { ProbeProgressStatus } from '../enums/probe-progress-status.enum';
import { ProbeProgressAuditEventDto } from './probe-progress-audit-event.dto';
import { ProbePointProgressDto } from './probe-point-progress.dto';

export interface ProbeProgressDto {
  probeProgressKey: string | null;
  memberKey: string;
  kurinKey: string;
  probeId: string;
  status: ProbeProgressStatus | keyof typeof ProbeProgressStatus;
  completedAtUtc: string | null;
  completedByUserKey: string | null;
  completedByName: string | null;
  completedByRole: string | null;
  verifiedAtUtc: string | null;
  verifiedByUserKey: string | null;
  verifiedByName: string | null;
  verifiedByRole: string | null;
  auditTrail: ProbeProgressAuditEventDto[];
  pointSignatures?: ProbePointProgressDto[];
}