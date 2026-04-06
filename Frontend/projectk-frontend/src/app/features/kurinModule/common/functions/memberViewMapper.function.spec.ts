import { MemberDto } from '../models/memberDto';
import { PlastLevel } from '../models/enums/plast-level.enum';
import { localizeLatestPlastLevel, mapMemberForView } from './memberViewMapper.function';

function createMember(partial: Partial<MemberDto>): MemberDto {
  return {
    memberKey: 'm1',
    groupKey: 'g1',
    kurinKey: 'k1',
    firstName: 'Test',
    middleName: 'T',
    lastName: 'User',
    email: 'test@example.com',
    phoneNumber: '123',
    dateOfBirth: null,
    plastLevelHistories: [],
    leadershipHistories: [],
    profilePhotoUrl: null,
    ...partial
  };
}

describe('memberViewMapper', () => {
  it('localizeLatestPlastLevel should localize regular level', () => {
    const member = createMember({ latestPlastLevel: PlastLevel.Uchasnyk });

    const result = localizeLatestPlastLevel(member);

    expect(result).toBe('пл. уч.');
  });

  it('localizeLatestPlastLevel should return st. pl. skob for Starshoplastun with Skob history', () => {
    const member = createMember({
      latestPlastLevel: PlastLevel.Starshoplastun,
      plastLevelHistories: [
        { plastLevel: PlastLevel.Entry },
        { plastLevel: PlastLevel.Skob }
      ]
    });

    const result = localizeLatestPlastLevel(member);

    expect(result).toBe('ст. пл. скоб');
  });

  it('localizeLatestPlastLevel should prioritize HetmanskiySkob suffix', () => {
    const member = createMember({
      latestPlastLevel: PlastLevel.Starshoplastun,
      plastLevelHistories: [
        { plastLevel: PlastLevel.Skob },
        { plastLevel: PlastLevel.HetmanskiySkob }
      ]
    });

    const result = localizeLatestPlastLevel(member);

    expect(result).toBe('ст. пл. гетьм. скоб');
  });

  it('mapMemberForView should inject latestPlastLevelDisplay', () => {
    const member = createMember({ latestPlastLevel: PlastLevel.SeniorDovirja });

    const result = mapMemberForView(member);

    expect(result.latestPlastLevelDisplay).toBe('пл. сен. дов.');
    expect(result.latestPlastLevel).toBe(PlastLevel.SeniorDovirja);
  });
});