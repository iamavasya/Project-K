import { BadgeProgressStatus } from '../models/enums/badge-progress-status.enum';
import { BadgeCatalogItemDto } from '../models/probes-and-badges/badgeCatalogItemDto';
import { BadgeProgressDto } from '../models/probes-and-badges/badgeProgressDto';
import { MemberSkillItemView } from '../models/probes-and-badges/memberSkillItemView';
import { MemberSkillsSummaryView } from '../models/probes-and-badges/memberSkillsSummaryView';

const BADGES_IMAGES_BASE_PATH = '/badges_images';

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
    return imagePath;
  }

  const normalized = imagePath.startsWith('/') ? imagePath.slice(1) : imagePath;
  if (normalized.startsWith('badges_images/')) {
    return `/${normalized}`;
  }

  return `${BADGES_IMAGES_BASE_PATH}/${normalized}`;
}

export function mapBadgeProgressToSkillView(
  progress: BadgeProgressDto,
  badgeById: ReadonlyMap<string, BadgeCatalogItemDto>
): MemberSkillItemView {
  const badge = badgeById.get(progress.badgeId);

  return {
    badgeId: progress.badgeId,
    title: badge?.title ?? progress.badgeId,
    imageUrl: resolveBadgeImageUrl(badge?.imagePath ?? null),
    status: progress.status,
    submittedAtUtc: progress.submittedAtUtc,
    reviewedAtUtc: progress.reviewedAtUtc,
    isPendingConfirmation: progress.status === BadgeProgressStatus.Submitted
  };
}

export function buildMemberSkillsSummary(
  progresses: BadgeProgressDto[],
  badges: BadgeCatalogItemDto[],
  recentConfirmedLimit = 8
): MemberSkillsSummaryView {
  const safeLimit = Math.max(0, recentConfirmedLimit);
  const badgeById = new Map<string, BadgeCatalogItemDto>(badges.map(badge => [badge.id, badge]));

  const confirmed = progresses
    .filter(progress => progress.status === BadgeProgressStatus.Confirmed)
    .sort((left, right) => toUnixTime(right.reviewedAtUtc) - toUnixTime(left.reviewedAtUtc))
    .map(progress => mapBadgeProgressToSkillView(progress, badgeById));

  const pending = progresses
    .filter(progress => progress.status === BadgeProgressStatus.Submitted)
    .sort((left, right) => toUnixTime(right.submittedAtUtc) - toUnixTime(left.submittedAtUtc))
    .map(progress => mapBadgeProgressToSkillView(progress, badgeById));

  return {
    recentConfirmed: confirmed,
    pendingConfirmation: pending,
    orderedPreview: [...confirmed.slice(0, safeLimit), ...pending]
  };
}