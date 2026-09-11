import { ComponentFixture, TestBed } from '@angular/core/testing';

import { MemberMembershipsTileComponent } from './member-memberships-tile';
import { MembershipDto } from '../../../../models/membership.dto';
import { KurinBranch } from '../../../../models/enums/kurin-branch.enum';
import { MembershipKind } from '../../../../models/enums/membership-kind.enum';

describe('MemberMembershipsTileComponent', () => {
  let fixture: ComponentFixture<MemberMembershipsTileComponent>;
  let component: MemberMembershipsTileComponent;

  const membership = (over: Partial<MembershipDto>): MembershipDto => ({
    membershipKey: crypto.randomUUID(),
    kurinKey: crypto.randomUUID(),
    kurinNumber: 7,
    branch: KurinBranch.UPYu,
    kurinNamedAfter: null,
    groupKey: null,
    groupName: null,
    kind: MembershipKind.Youth,
    joinedAtUtc: '2022-09-01T00:00:00Z',
    leftAtUtc: null,
    isCurrent: true,
    ...over
  });

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [MemberMembershipsTileComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(MemberMembershipsTileComponent);
    component = fixture.componentInstance;
  });

  it('розділяє теперішні курені й ті, які людина покинула', () => {
    const here = membership({});
    const left = membership({ isCurrent: false, leftAtUtc: '2024-06-01T00:00:00Z', kurinNumber: 42 });
    fixture.componentRef.setInput('memberships', [here, left]);
    fixture.detectChanges();

    expect(component.current()).toEqual([here]);
    expect(component.past()).toEqual([left]);
  });

  it('теперішнє членство показує лише рік початку', () => {
    expect(component.period(membership({}))).toBe('з 2022');
  });

  it('закрите членство показує обидва роки, а однорічне — один', () => {
    expect(component.period(membership({ isCurrent: false, leftAtUtc: '2024-06-01T00:00:00Z' })))
      .toBe('2022 — 2024');
    expect(component.period(membership({
      isCurrent: false,
      joinedAtUtc: '2024-01-01T00:00:00Z',
      leftAtUtc: '2024-06-01T00:00:00Z'
    }))).toBe('2024');
  });
});
