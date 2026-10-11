import { MemberLookupDto } from './kurin.models';
import { addableToGroup, mentorChanges, placedIn, placementLine } from './group-people';

const person = (memberKey: string, lastName: string, extra: Partial<MemberLookupDto> = {}): MemberLookupDto => ({
  memberKey, firstName: 'Імʼя', lastName, middleName: null, ...extra,
});
const NONE = '00000000-0000-0000-0000-000000000000';

describe('people on a гурток', () => {
  it('assigns the newly ticked виховники and revokes the unticked', () => {
    expect(mentorChanges(['u1', 'u2'], ['u2', 'u3'])).toEqual({ assign: ['u3'], revoke: ['u1'] });
    expect(mentorChanges(['u1'], ['u1'])).toEqual({ assign: [], revoke: [] });
  });

  it('offers everyone not yet here, those in no гурток first', () => {
    const here = person('m1', 'Андрієнко', { groupKey: 'g1', groupName: 'Соколи' });
    const elsewhere = person('m2', 'Бойко', { groupKey: 'g2', groupName: 'Орли' });
    const loose = person('m3', 'Яремко', { groupKey: NONE });
    const alsoLoose = person('m4', 'Гнатюк', { groupKey: null });
    const list = addableToGroup([here, elsewhere, loose, alsoLoose], 'g1');
    expect(list.map((m) => m.memberKey)).toEqual(['m4', 'm3', 'm2']);
    // Someone the group list already shows is not offered, whatever the kurin list says.
    expect(addableToGroup([elsewhere], 'g1', [elsewhere])).toEqual([]);
    expect(placedIn(loose)).toBeNull();
    expect(placementLine(elsewhere)).toBe('Зараз: Орли');
    expect(placementLine(loose)).toBe('Без гуртка');
  });
});
