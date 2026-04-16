import { MemberProbeDetailPointRowView } from '../models/probes-and-badges/memberProbeDetailPointRowView';
import { GroupedProbeDto } from '../models/probes-and-badges/groupedProbeDto';
import { ProbeProgressDto } from '../models/probes-and-badges/probeProgressDto';
import { ProbeProgressStatus } from '../models/enums/probe-progress-status.enum';
import { normalizeProbeProgressStatus } from './memberProbeRowsViewMapper.function';

function isSignedProbeStatus(status: ProbeProgressStatus): boolean {
  return status === ProbeProgressStatus.Completed || status === ProbeProgressStatus.Verified;
}

export function buildMemberProbeDetailPointRows(
  groupedProbe: GroupedProbeDto | null,
  progress: ProbeProgressDto | null
): MemberProbeDetailPointRowView[] {
  if (!groupedProbe) {
    return [];
  }

  const normalizedStatus = normalizeProbeProgressStatus(progress?.status ?? ProbeProgressStatus.NotStarted);
  const isSigned = isSignedProbeStatus(normalizedStatus);
  const signedByName = progress?.verifiedByName ?? progress?.completedByName ?? null;
  const signedByRole = progress?.verifiedByRole ?? progress?.completedByRole ?? null;
  const signedAtUtc = progress?.verifiedAtUtc ?? progress?.completedAtUtc ?? null;

  return groupedProbe.sections.flatMap(section =>
    section.points.map(point => ({
      sectionId: section.id,
      sectionCode: section.code,
      sectionTitle: section.title,
      pointId: point.id,
      pointTitle: point.title,
      isSigned,
      signedByName: isSigned ? signedByName : null,
      signedByRole: isSigned ? signedByRole : null,
      signedAtUtc: isSigned ? signedAtUtc : null
    }))
  );
}
