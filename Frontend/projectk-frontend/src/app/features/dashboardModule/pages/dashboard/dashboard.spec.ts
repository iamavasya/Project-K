import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { Router, provideRouter } from '@angular/router';
import { MessageService } from '@openng/optimus-ui/api';
import { of } from 'rxjs';
import { AuthState } from '../../../authModule/models/auth-state.model';
import { AuthService } from '../../../authModule/services/auth-service/auth.service';
import { KurinBranch } from '../../../kurinModule/models/enums/kurin-branch.enum';
import { MembershipKind } from '../../../kurinModule/models/enums/membership-kind.enum';
import { KurinScopeOption } from '../../../kurinModule/models/kurin-scope-option.model';
import { MemberDto } from '../../../kurinModule/models/member.dto';
import { MembershipDto } from '../../../kurinModule/models/membership.dto';
import { MemberService } from '../../../kurinModule/services/member-service/member.service';
import { DashboardComponent } from './dashboard';

describe('DashboardComponent', () => {
  let fixture: ComponentFixture<DashboardComponent>;
  let component: DashboardComponent;
  let auth: jasmine.SpyObj<AuthService>;
  let members: jasmine.SpyObj<MemberService>;
  let router: Router;

  const state: AuthState = {
    userKey: 'u1', memberKey: 'm1', email: 'o@x', isAdmin: false, permissions: [], roles: [], kurinKey: 'k1', accessToken: 't'
  };

  const member = {
    memberKey: 'm1', firstName: 'Оксана', lastName: 'Паливода', middleName: '', email: 'o@x', phoneNumber: '', dateOfBirth: null,
    groupKey: 'g1', groupName: 'Соколи', kurinKey: 'k1', latestPlastLevelDisplay: 'учасниця', plastLevelHistories: [],
    leadershipHistories: [
      { leadershipHistoryKey: 'h1', leadershipKey: 'l1', role: 'Suddya', leadershipType: 'Group', groupName: 'Соколи', startDate: '2026-09-01', endDate: null, member: { memberKey: 'm1', fullName: 'Оксана' } },
      { leadershipHistoryKey: 'h2', leadershipKey: 'l2', role: 'Pysar', leadershipType: 'Kurin', groupName: null, startDate: '2025-09-01', endDate: '2026-08-31', member: { memberKey: 'm1', fullName: 'Оксана' } }
    ],
    profilePhotoUrl: null
  } as unknown as MemberDto;

  const kurins: KurinScopeOption[] = [
    { kurinKey: 'k2', kurinNumber: 7, branch: KurinBranch.USP, namedAfter: null, kind: MembershipKind.Staff },
    { kurinKey: 'k1', kurinNumber: 1, branch: KurinBranch.UPYu, namedAfter: 'ім. Івана Богуна', kind: MembershipKind.Youth }
  ];

  const memberships: MembershipDto[] = [
    { membershipKey: 'ms1', kurinKey: 'k1', kurinNumber: 1, branch: KurinBranch.UPYu, groupKey: 'g1', groupName: 'Соколи', kind: MembershipKind.Youth, joinedAtUtc: '2025-09-01', isCurrent: true }
  ];

  beforeEach(() => {
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['getAuthStateValue', 'getKurinScopeOptions', 'setKurinScope', 'getAuthState']);
    auth.getAuthStateValue.and.returnValue(state);
    auth.getAuthState.and.returnValue(of(state));
    auth.getKurinScopeOptions.and.returnValue(of(kurins));
    auth.setKurinScope.and.returnValue(of({ ...state, kurinKey: 'k2' }));
    members = jasmine.createSpyObj<MemberService>('MemberService', ['getByKey', 'getMemberships']);
    members.getByKey.and.returnValue(of(member));
    members.getMemberships.and.returnValue(of(memberships));

    TestBed.configureTestingModule({
      imports: [DashboardComponent],
      providers: [
        provideNoopAnimations(),
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth },
        { provide: MemberService, useValue: members },
        { provide: MessageService, useValue: jasmine.createSpyObj<MessageService>('MessageService', ['add']) }
      ]
    });
    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  it('greets the person by name and shows both tiles', () => {
    expect(text()).toContain('Оксана');
    expect(text()).toContain('Мій профіль');
    expect(text()).toContain('Мої курені');
    expect(text()).toContain('Соколи');
  });

  it('lists the offices held now in the kurin acted in, not the ones that ended', () => {
    expect(component.currentOffices()).toEqual(['Суддя · Соколи']);
  });

  it('opens the current kurin as it is, and switches the scope for another', () => {
    component.openKurin(kurins[1]);
    expect(auth.setKurinScope).not.toHaveBeenCalled();
    expect(router.navigate).toHaveBeenCalledWith(['/kurin', 'k1']);

    component.openKurin(kurins[0]);
    expect(auth.setKurinScope).toHaveBeenCalledWith('k2');
    expect(router.navigate).toHaveBeenCalledWith(['/kurin', 'k2']);
  });
});
