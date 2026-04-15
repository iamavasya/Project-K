import { BadgeProgressStatus } from '../models/enums/badge-progress-status.enum';
import { BadgeCatalogItemDto } from '../models/probes-and-badges/badgeCatalogItemDto';
import { BadgeProgressDto } from '../models/probes-and-badges/badgeProgressDto';
import { buildMemberSkillsSummary, resolveBadgeImageUrl } from './memberSkillsViewMapper.function';

function createBadge(overrides: Partial<BadgeCatalogItemDto>): BadgeCatalogItemDto {
  return {
    id: 'badge-1',
    title: 'Sample Badge',
    imagePath: 'badges/sample.png',
    country: 'UA',
    specialization: 'Scout',
    status: 'active',
    level: 1,
    lastUpdated: '2026-04-01T00:00:00Z',
    seekerRequirements: '',
    instructorRequirements: '',
    fixNotes: [],
    ...overrides
  };
}

function createProgress(overrides: Partial<BadgeProgressDto>): BadgeProgressDto {
  return {
    badgeProgressKey: 'progress-1',
    memberKey: 'member-1',
    kurinKey: 'kurin-1',
    badgeId: 'badge-1',
    status: BadgeProgressStatus.Submitted,
    submittedAtUtc: null,
    reviewedAtUtc: null,
    reviewedByUserKey: null,
    reviewedByName: null,
    reviewedByRole: null,
    reviewNote: null,
    auditTrail: [],
    ...overrides
  };
}

describe('memberSkillsViewMapper', () => {
  it('should build ordered preview with recent confirmed first and pending at the end', () => {
    const badges = [
      createBadge({ id: 'badge-1', title: 'A' }),
      createBadge({ id: 'badge-2', title: 'B' }),
      createBadge({ id: 'badge-3', title: 'C' }),
      createBadge({ id: 'badge-4', title: 'D' })
    ];

    const progresses = [
      createProgress({ badgeId: 'badge-1', status: BadgeProgressStatus.Confirmed, reviewedAtUtc: '2026-04-10T00:00:00Z' }),
      createProgress({ badgeId: 'badge-2', status: BadgeProgressStatus.Confirmed, reviewedAtUtc: '2026-04-12T00:00:00Z' }),
      createProgress({ badgeId: 'badge-3', status: BadgeProgressStatus.Confirmed, reviewedAtUtc: '2026-04-11T00:00:00Z' }),
      createProgress({ badgeId: 'badge-4', status: BadgeProgressStatus.Submitted, submittedAtUtc: '2026-04-13T00:00:00Z' })
    ];

    const summary = buildMemberSkillsSummary(progresses, badges, 2);

    expect(summary.recentConfirmed.map(item => item.badgeId)).toEqual(['badge-2', 'badge-3', 'badge-1']);
    expect(summary.pendingConfirmation.map(item => item.badgeId)).toEqual(['badge-4']);
    expect(summary.orderedPreview.map(item => item.badgeId)).toEqual(['badge-2', 'badge-3', 'badge-4']);
  });

  it('should fallback to badgeId when catalog entry does not exist', () => {
    const summary = buildMemberSkillsSummary(
      [createProgress({ badgeId: 'unknown-badge', status: BadgeProgressStatus.Submitted })],
      []
    );

    expect(summary.pendingConfirmation[0].title).toBe('unknown-badge');
  });

  it('resolveBadgeImageUrl should keep absolute urls and normalize relative paths', () => {
    expect(resolveBadgeImageUrl('https://cdn.site/badge.png')).toBe('https://cdn.site/badge.png');
    expect(resolveBadgeImageUrl('/badges_images/badge.png')).toBe('/badges_images/badge.png');
    expect(resolveBadgeImageUrl('badges/badge.png')).toBe('/badges_images/badges/badge.png');
    expect(resolveBadgeImageUrl('badges_images/badge.png')).toBe('/badges_images/badge.png');
  });
});