import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';

import { AuthService } from '../../authModule/services/authService/auth.service';
import { KurinService } from '../common/services/kurin-service/kurin.service';
import { MemberService } from '../common/services/member-service/member.service';
import { MemberDto } from '../common/models/memberDto';
import { KurinBranch } from '../common/models/enums/kurin-branch.enum';
import { PlastLevel } from '../common/models/enums/plast-level.enum';
import { RegistryComponent } from './registry';

function member(partial: Partial<MemberDto>): MemberDto {
  return {
    memberKey: partial.lastName ?? 'k',
    groupKey: 'g',
    kurinKey: 'kurin-1',
    firstName: 'Тест',
    middleName: '',
    lastName: 'Особа',
    email: 'a@b.c',
    phoneNumber: '0500000000',
    dateOfBirth: null,
    plastLevelHistories: [],
    leadershipHistories: [],
    profilePhotoUrl: null,
    ...partial
  };
}

describe('RegistryComponent', () => {
  let component: RegistryComponent;

  function load(people: MemberDto[], branch = KurinBranch.UPYu): void {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [RegistryComponent],
      providers: [
        { provide: MemberService, useValue: { getAll: () => of(people) } },
        {
          provide: KurinService,
          useValue: { getByKey: () => of({ kurinKey: 'kurin-1', number: 3, branch }) }
        },
        { provide: AuthService, useValue: { getAuthState: () => of({ kurinKey: 'kurin-1' }) } }
      ]
    });

    localStorage.clear();
    const fixture = TestBed.createComponent(RegistryComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  afterEach(() => localStorage.clear());

  /**
   * Розділяє уряд у кадрі виховників, і бекенд уже сказав, у кого він є. Гуртковий теж має уряд і
   * теж юнак — якби ділили за «має уряд», таблиця юнаків втратила б саме тих, хто веде гуртки.
   */
  it('should keep юнаки and впорядники in separate tables', () => {
    load([
      member({ lastName: 'Юнак' }),
      member({ lastName: 'Гуркова', userRole: 'Group.Hurtkoviy' }),
      member({ lastName: 'Виховна', isStaff: true, mentoredGroupNames: ['Alpha'] })
    ]);

    expect(component.youth().map(person => person.lastName)).toEqual(['Гуркова', 'Юнак']);
    expect(component.staff().map(person => person.lastName)).toEqual(['Виховна']);
  });

  it('should lead the staff table with the гуртки they mentor and drop their own', () => {
    load([member({ lastName: 'Виховна', isStaff: true, mentoredGroupNames: ['Alpha', 'Beta'] })]);

    const ids = component.staffColumns().map(column => column.id);

    expect(ids[0]).toBe('mentoredGroups');
    expect(ids).not.toContain('groupName');

    const assignment = component.staffColumns()[0];
    expect(component.cell(component.staff()[0], assignment)).toBe('Alpha, Beta');
  });

  it('should not offer the закріплення column in the chooser', () => {
    load([]);

    expect(component.allColumns.map(column => column.id)).not.toContain('mentoredGroups');
  });

  it('should count only юнаки, one row per ступінь', () => {
    load([
      member({ lastName: 'Один', latestPlastLevel: PlastLevel.Uchasnyk }),
      member({ lastName: 'Два', latestPlastLevel: PlastLevel.Uchasnyk }),
      member({ lastName: 'Три', latestPlastLevel: PlastLevel.Skob }),
      member({ lastName: 'Виховна', latestPlastLevel: PlastLevel.Uchasnyk, isStaff: true })
    ]);

    const counts = new Map(component.tally().map(row => [row.label, row.count]));

    expect(counts.get('пл. уч.')).toBe(2);
    expect(counts.get('пл. скоб / вірл.')).toBe(1);
    expect(counts.get('Разом')).toBe(3);
  });

  /** Незаписаний ступінь — саме те, заради чого цю табличку й читають. */
  it('should show people with no ступінь rather than losing them', () => {
    load([member({ lastName: 'Без' }), member({ lastName: 'Учасник', latestPlastLevel: PlastLevel.Uchasnyk })]);

    const counts = new Map(component.tally().map(row => [row.label, row.count]));

    expect(counts.get('Без ступеня / інший')).toBe(1);
    expect(counts.get('Разом')).toBe(2);
  });

  it('should count a УПС kurin on its own ladder', () => {
    load([member({ lastName: 'Сеньйор', latestPlastLevel: PlastLevel.SeniorPratsi })], KurinBranch.UPS);

    const counts = new Map(component.tally().map(row => [row.label, row.count]));

    expect(counts.get('Сен. праці')).toBe(1);
    expect(counts.has('Без ступеня / інший')).toBeFalse();
  });

  it('should search across both tables at once', () => {
    load([
      member({ lastName: 'Юнак', groupName: 'Alpha' }),
      member({ lastName: 'Виховна', isStaff: true, mentoredGroupNames: ['Alpha'] }),
      member({ lastName: 'Стороння', groupName: 'Beta' })
    ]);

    component.search.set('alpha');

    expect(component.youth().map(person => person.lastName)).toEqual(['Юнак']);
    expect(component.staff().map(person => person.lastName)).toEqual(['Виховна']);
  });
});
