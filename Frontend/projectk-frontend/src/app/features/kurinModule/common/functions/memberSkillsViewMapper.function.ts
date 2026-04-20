import { BadgeProgressStatus } from '../models/enums/badge-progress-status.enum';
import { BadgeCatalogItemDto } from '../models/probes-and-badges/badgeCatalogItemDto';
import { BadgeProgressDto } from '../models/probes-and-badges/badgeProgressDto';
import { MemberSkillItemView } from '../models/probes-and-badges/memberSkillItemView';
import { MemberSkillsSummaryView } from '../models/probes-and-badges/memberSkillsSummaryView';
import { environment } from '../../../../../environments/environment';

const BADGES_IMAGES_BASE_PATH = '/badges_images';
const API_ORIGIN = resolveApiOrigin(environment.apiUrl);

export type BadgeProgressStatusLike = BadgeProgressStatus | string | number;

function resolveApiOrigin(apiUrl: string): string {
  try {
    return new URL(apiUrl).origin;
  } catch {
    return '';
  }
}

function withApiOrigin(path: string): string {
  return API_ORIGIN ? `${API_ORIGIN}${path}` : path;
}

function toUnixTime(value: string | null): number {
  if (!value) {
    return 0;
  }

  const parsed = Date.parse(value);
  return Number.isNaN(parsed) ? 0 : parsed;
}

export function resolveBadgeImageUrl(imagePath: string | null | undefined): string | null {
  if (!imagePath) {
    return null;
  }

  if (imagePath.startsWith('http://') || imagePath.startsWith('https://') || imagePath.startsWith('data:')) {
    return imagePath;
  }

  if (imagePath.startsWith('/badges_images/')) {
    return withApiOrigin(imagePath);
  }

  const normalized = imagePath.startsWith('/') ? imagePath.slice(1) : imagePath;
  if (normalized.startsWith('badges_images/')) {
    return withApiOrigin(`/${normalized}`);
  }

  return withApiOrigin(`${BADGES_IMAGES_BASE_PATH}/${normalized}`);
}

export function mapBadgeProgressToSkillView(
  progress: BadgeProgressDto,
  badgeById: ReadonlyMap<string, BadgeCatalogItemDto>
): MemberSkillItemView {
  const badge = badgeById.get(progress.badgeId);
  const normalizedStatus = normalizeBadgeProgressStatus(progress.status);

  return {
    badgeId: progress.badgeId,
    title: badge?.title ?? progress.badgeId,
    imageUrl: resolveBadgeImageUrl(badge?.imagePath ?? null),
    status: normalizedStatus,
    submittedAtUtc: progress.submittedAtUtc,
    reviewedAtUtc: progress.reviewedAtUtc,
    isPendingConfirmation: normalizedStatus === BadgeProgressStatus.Submitted
  };
}

export function normalizeBadgeProgressStatus(status: BadgeProgressStatusLike): BadgeProgressStatus {
  if (typeof status === 'number' && BadgeProgressStatus[status] !== undefined) {
    return status as BadgeProgressStatus;
  }

  if (typeof status === 'string') {
    const enumValue = BadgeProgressStatus[status as keyof typeof BadgeProgressStatus];
    if (typeof enumValue === 'number') {
      return enumValue;
    }
  }

  return BadgeProgressStatus.Draft;
}

export function buildMemberSkillsSummary(
  progresses: BadgeProgressDto[],
  badges: BadgeCatalogItemDto[],
  recentConfirmedLimit = 8
): MemberSkillsSummaryView {
  const safeLimit = Math.max(0, recentConfirmedLimit);
  const badgeById = new Map<string, BadgeCatalogItemDto>(badges.map(badge => [badge.id, badge]));

  const confirmed = progresses
    .filter(progress => normalizeBadgeProgressStatus(progress.status) === BadgeProgressStatus.Confirmed)
    .sort((left, right) => toUnixTime(right.reviewedAtUtc) - toUnixTime(left.reviewedAtUtc))
    .map(progress => mapBadgeProgressToSkillView(progress, badgeById));

  const pending = progresses
    .filter(progress => normalizeBadgeProgressStatus(progress.status) === BadgeProgressStatus.Submitted)
    .sort((left, right) => toUnixTime(right.submittedAtUtc) - toUnixTime(left.submittedAtUtc))
    .map(progress => mapBadgeProgressToSkillView(progress, badgeById));

  return {
    recentConfirmed: confirmed,
    pendingConfirmation: pending,
    orderedPreview: [...confirmed.slice(0, safeLimit), ...pending]
  };
}