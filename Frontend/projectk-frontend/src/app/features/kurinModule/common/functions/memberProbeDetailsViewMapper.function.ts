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
  const pointSignatures = progress?.pointSignatures ?? [];
  const hasPointLevelSignatures = pointSignatures.length > 0;
  const pointSignaturesByPointId = new Map(pointSignatures.map(signature => [signature.pointId, signature]));

  const signedByUserKey = progress?.verifiedByUserKey ?? progress?.completedByUserKey ?? null;
  const signedByName = progress?.verifiedByName ?? progress?.completedByName ?? null;
  const signedByRole = progress?.verifiedByRole ?? progress?.completedByRole ?? null;
  const signedAtUtc = progress?.verifiedAtUtc ?? progress?.completedAtUtc ?? null;

  return groupedProbe.sections.flatMap(section =>
    section.points.map(point => {
      const pointSignature = pointSignaturesByPointId.get(point.id);
      const isPointSigned = hasPointLevelSignatures
        ? (pointSignature?.isSigned ?? false)
        : isSigned;

      const pointSignedByUserKey = pointSignature?.signedByUserKey ?? signedByUserKey;
      const pointSignedByName = pointSignature?.signedByName ?? signedByName;
      const pointSignedByRole = pointSignature?.signedByRole ?? signedByRole;
      const pointSignedAtUtc = pointSignature?.signedAtUtc ?? signedAtUtc;

      return {
        sectionId: section.id,
        sectionCode: section.code,
        sectionTitle: section.title,
        pointId: point.id,
        pointTitle: point.title,
        isSigned: isPointSigned,
        signedByUserKey: isPointSigned ? pointSignedByUserKey : null,
        signedByName: isPointSigned ? pointSignedByName : null,
        signedByRole: isPointSigned ? pointSignedByRole : null,
        signedAtUtc: isPointSigned ? pointSignedAtUtc : null
      };
    })
  );
}
