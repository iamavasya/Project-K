import { ProbeProgressStatus } from '../enums/probe-progress-status.enum';

export interface MemberProbeRowView {
  probeId: string;
  label: string;
  title: string;
  status: ProbeProgressStatus;
  completedAtUtc: string | null;
  isCompleted: boolean;
  isDisabled: boolean;
  canOpenDetails: boolean;
  pointsCount: number | null;
  sectionsCount: number | null;
}