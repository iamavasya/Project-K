import { ProbeProgressStatus } from '../models/enums/probe-progress-status.enum';
import { MemberProbeRowView } from '../models/probes-and-badges/memberProbeRowView';
import { ProbeProgressDto } from '../models/probes-and-badges/probeProgressDto';
import { ProbeSummaryDto } from '../models/probes-and-badges/probeSummaryDto';

export type ProbeProgressStatusLike = ProbeProgressStatus | keyof typeof ProbeProgressStatus | string | number;

interface ProbeRowTemplate {
  probeId: string;
  label: string;
  isPolicyDisabled: boolean;
}

const PROBE_ROW_TEMPLATES: ProbeRowTemplate[] = [
  { probeId: 'probe-1', label: 'Перша проба', isPolicyDisabled: false },
  { probeId: 'probe-2', label: 'Друга проба', isPolicyDisabled: false },
  { probeId: 'probe-3', label: 'Третя проба', isPolicyDisabled: true }
];

function resolveCompletedAtUtc(progress: ProbeProgressDto | undefined): string | null {
  if (!progress) {
    return null;
  }

  return progress.completedAtUtc ?? progress.verifiedAtUtc;
}

function isCompletedStatus(status: ProbeProgressStatus): boolean {
  return status === ProbeProgressStatus.Completed || status === ProbeProgressStatus.Verified;
}

export function normalizeProbeProgressStatus(status: ProbeProgressStatusLike): ProbeProgressStatus {
  if (typeof status === 'number' && ProbeProgressStatus[status] !== undefined) {
    return status as ProbeProgressStatus;
  }

  if (typeof status === 'string') {
    const enumValue = ProbeProgressStatus[status as keyof typeof ProbeProgressStatus];
    if (typeof enumValue === 'number') {
      return enumValue;
    }
  }

  return ProbeProgressStatus.NotStarted;
}

export function buildMemberProbeRows(
  probes: ProbeSummaryDto[],
  progresses: ProbeProgressDto[]
): MemberProbeRowView[] {
  const probeById = new Map<string, ProbeSummaryDto>(probes.map(probe => [probe.id, probe]));
  const progressByProbeId = new Map<string, ProbeProgressDto>(progresses.map(progress => [progress.probeId, progress]));

  const rows = PROBE_ROW_TEMPLATES.map(template => {
    const probe = probeById.get(template.probeId);
    const progress = progressByProbeId.get(template.probeId);
    const status = normalizeProbeProgressStatus(progress?.status ?? ProbeProgressStatus.NotStarted);

    return {
      probeId: probe?.id ?? template.probeId,
      label: template.label,
      title: probe?.title ?? template.label,
      status,
      completedAtUtc: resolveCompletedAtUtc(progress),
      isCompleted: isCompletedStatus(status),
      isDisabled: template.isPolicyDisabled,
      canOpenDetails: !template.isPolicyDisabled && !!probe,
      pointsCount: probe?.pointsCount ?? null,
      sectionsCount: probe?.sectionsCount ?? null
    };
  });

  const firstProbe = rows[0];
  const isFirstProbeClosed = !!firstProbe && firstProbe.isCompleted;

  return rows.map(row => {
    if (row.probeId !== 'probe-2') {
      return row;
    }

    const hasOwnProgress = row.status !== ProbeProgressStatus.NotStarted || !!row.completedAtUtc;
    const isUnlocked = isFirstProbeClosed || hasOwnProgress;
    const isDisabled = !isUnlocked;

    return {
      ...row,
      isDisabled,
      canOpenDetails: !isDisabled && row.canOpenDetails
    };
  });
}