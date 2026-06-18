import { LeadershipRole } from '../models/enums/leadership-role.enum';
import { LeadershipHistoryDto } from '../models/requests/leadership/leadershipDto';
import { compareLeadershipHistoriesByDefault, getLeadershipRoleSortWeight } from './leadershipRoleOrder.function';

describe('leadershipRoleOrder', () => {
  it('should prioritize zvyazkovyi before the remaining leadership roles', () => {
    expect(getLeadershipRoleSortWeight(LeadershipRole.Zvyazkovyi))
      .toBeLessThan(getLeadershipRoleSortWeight(LeadershipRole.Kurinnuy));
    expect(getLeadershipRoleSortWeight(LeadershipRole.Kurinnuy))
      .toBeLessThan(getLeadershipRoleSortWeight(LeadershipRole.Hurtkoviy));
    expect(getLeadershipRoleSortWeight(LeadershipRole.Pysar))
      .toBeLessThan(getLeadershipRoleSortWeight(LeadershipRole.Skarbnyk));
    expect(getLeadershipRoleSortWeight(LeadershipRole.Instruktor))
      .toBeLessThan(getLeadershipRoleSortWeight(LeadershipRole.Vykhovnyk));
  });

  it('should sort active histories by role weight before start date', () => {
    const histories = [
      history(LeadershipRole.Pysar, '2026-01-01'),
      history(LeadershipRole.Kurinnuy, '2020-01-01'),
      history(LeadershipRole.Suddya, '2025-01-01')
    ];

    histories.sort(compareLeadershipHistoriesByDefault);

    expect(histories.map(item => item.role)).toEqual([
      LeadershipRole.Kurinnuy,
      LeadershipRole.Suddya,
      LeadershipRole.Pysar
    ]);
  });

  it('should keep active histories before archived histories', () => {
    const histories = [
      history(LeadershipRole.Kurinnuy, '2026-01-01', '2026-06-01'),
      history(LeadershipRole.Pysar, '2020-01-01')
    ];

    histories.sort(compareLeadershipHistoriesByDefault);

    expect(histories[0].role).toBe(LeadershipRole.Pysar);
    expect(histories[0].endDate).toBeNull();
  });
});

function history(role: LeadershipRole, startDate: string, endDate: string | null = null): LeadershipHistoryDto {
  return {
    leadershipHistoryKey: `${role}-${startDate}`,
    leadershipKey: 'l1',
    role,
    startDate,
    endDate,
    member: {
      memberKey: `${role}-member`,
      firstName: role,
      lastName: 'Member',
      middleName: null
    }
  };
}
