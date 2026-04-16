import { ProbeProgressStatus } from '../models/enums/probe-progress-status.enum';
import { ProbeProgressDto } from '../models/probes-and-badges/probeProgressDto';
import { ProbeSummaryDto } from '../models/probes-and-badges/probeSummaryDto';
import { buildMemberProbeRows, normalizeProbeProgressStatus } from './memberProbeRowsViewMapper.function';

function createProbeSummary(overrides: Partial<ProbeSummaryDto>): ProbeSummaryDto {
  return {
    id: 'probe-1',
    title: 'Перша проба (Скобине крило)',
    pointsCount: 10,
    sectionsCount: 3,
    ...overrides
  };
}

function createProbeProgress(overrides: Partial<ProbeProgressDto>): ProbeProgressDto {
  return {
    probeProgressKey: 'progress-1',
    memberKey: 'member-1',
    kurinKey: 'kurin-1',
    probeId: 'probe-1',
    status: ProbeProgressStatus.NotStarted,
    completedAtUtc: null,
    completedByUserKey: null,
    completedByName: null,
    completedByRole: null,
    verifiedAtUtc: null,
    verifiedByUserKey: null,
    verifiedByName: null,
    verifiedByRole: null,
    auditTrail: [],
    ...overrides
  };
}

describe('memberProbeRowsViewMapper', () => {
  it('should return 3 rows in canonical order and keep third probe disabled', () => {
    const probes = [
      createProbeSummary({ id: 'probe-2', title: 'Друга проба', pointsCount: 12, sectionsCount: 2 }),
      createProbeSummary({ id: 'probe-1', title: 'Перша проба', pointsCount: 11, sectionsCount: 3 }),
      createProbeSummary({ id: 'probe-3', title: 'Третя проба', pointsCount: 15, sectionsCount: 4 })
    ];

    const progresses = [
      createProbeProgress({ probeId: 'probe-1', status: ProbeProgressStatus.Completed, completedAtUtc: '2026-04-10T00:00:00Z' }),
      createProbeProgress({ probeId: 'probe-2', status: ProbeProgressStatus.InProgress })
    ];

    const rows = buildMemberProbeRows(probes, progresses);

    expect(rows.map(row => row.probeId)).toEqual(['probe-1', 'probe-2', 'probe-3']);
    expect(rows[0].isCompleted).toBeTrue();
    expect(rows[0].completedAtUtc).toBe('2026-04-10T00:00:00Z');
    expect(rows[0].canOpenDetails).toBeTrue();

    expect(rows[2].isDisabled).toBeTrue();
    expect(rows[2].canOpenDetails).toBeFalse();
  });

  it('should use fallback labels and not-started status when probe data is missing', () => {
    const rows = buildMemberProbeRows([], []);

    expect(rows[0].title).toBe('Перша проба');
    expect(rows[0].status).toBe(ProbeProgressStatus.NotStarted);
    expect(rows[0].canOpenDetails).toBeFalse();
  });

  it('normalizeProbeProgressStatus should support string enum values from backend JSON', () => {
    expect(normalizeProbeProgressStatus('InProgress')).toBe(ProbeProgressStatus.InProgress);
    expect(normalizeProbeProgressStatus('Verified')).toBe(ProbeProgressStatus.Verified);
  });

  it('should mark probe completed when backend status is provided as string', () => {
    const rows = buildMemberProbeRows(
      [createProbeSummary({ id: 'probe-1', title: 'Перша проба' })],
      [createProbeProgress({ probeId: 'probe-1', status: 'Completed' as unknown as ProbeProgressStatus })]
    );

    expect(rows[0].isCompleted).toBeTrue();
    expect(rows[0].status).toBe(ProbeProgressStatus.Completed);
  });
});