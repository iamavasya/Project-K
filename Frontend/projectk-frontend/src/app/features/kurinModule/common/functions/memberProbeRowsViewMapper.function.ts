import { ProbeProgressStatus } from '../models/enums/probe-progress-status.enum';
import { MemberProbeRowView } from '../models/probes-and-badges/memberProbeRowView';
import { ProbeProgressDto } from '../models/probes-and-badges/probeProgressDto';
import { ProbeSummaryDto } from '../models/probes-and-badges/probeSummaryDto';

interface ProbeRowTemplate {
  probeId: string;
  label: string;
  isDisabled: boolean;
}

const PROBE_ROW_TEMPLATES: ProbeRowTemplate[] = [
  { probeId: 'probe-1', label: 'Перша проба', isDisabled: false },
  { probeId: 'probe-2', label: 'Друга проба', isDisabled: false },
  { probeId: 'probe-3', label: 'Третя проба', isDisabled: true }
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

export function buildMemberProbeRows(
  probes: ProbeSummaryDto[],
  progresses: ProbeProgressDto[]
): MemberProbeRowView[] {
  const probeById = new Map<string, ProbeSummaryDto>(probes.map(probe => [probe.id, probe]));
  const progressByProbeId = new Map<string, ProbeProgressDto>(progresses.map(progress => [progress.probeId, progress]));

  return PROBE_ROW_TEMPLATES.map(template => {
    const probe = probeById.get(template.probeId);
    const progress = progressByProbeId.get(template.probeId);
    const status = progress?.status ?? ProbeProgressStatus.NotStarted;

    return {
      probeId: template.probeId,
      label: template.label,
      title: probe?.title ?? template.label,
      status,
      completedAtUtc: resolveCompletedAtUtc(progress),
      isCompleted: isCompletedStatus(status),
      isDisabled: template.isDisabled,
      canOpenDetails: !template.isDisabled && !!probe,
      pointsCount: probe?.pointsCount ?? null,
      sectionsCount: probe?.sectionsCount ?? null
    };
  });
}