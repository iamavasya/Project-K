import { ProbeProgressStatus } from '../../enums/probe-progress-status.enum';

export interface UpdateProbeProgressStatusRequestDto {
  status: ProbeProgressStatus;
  note: string | null;
}