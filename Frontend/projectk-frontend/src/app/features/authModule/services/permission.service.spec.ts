import { TestBed } from '@angular/core/testing';
import { PermissionService } from './permission.service';
import { AuthService } from './authService/auth.service';
import { AuthState } from '../models/auth-state.model';

describe('PermissionService', () => {
  let service: PermissionService;
  let authService: jasmine.SpyObj<AuthService>;

  function setState(permissions: string[], isAdmin = false): void {
    const state: AuthState = {
      userKey: 'user-1',
      memberKey: 'member-1',
      email: 'user@example.com',
      isAdmin,
      permissions,
      roles: [],
      kurinKey: 'kurin-1',
      accessToken: 'token'
    };
    authService.getAuthStateValue.and.returnValue(state);
  }

  // Permission sets that reproduce the office tiers.
  const stewardPerms = [
    'Group:Manage:KurinWide', 'Group:Update:KurinWide', 'Member:Manage:KurinWide',
    'Kurin:Update:KurinWide', 'Leadership:Manage:KurinWide', 'PlanningSession:Manage:KurinWide'
  ];
  const leadPerms = [
    'Group:Manage:KurinWide', 'Group:Update:KurinWide', 'Member:Manage:KurinWide',
    'PlanningSession:Manage:KurinWide'
  ];
  const groupLeadPerms = ['Group:Update:OwnGroups', 'Member:Update:OwnGroups'];
  const memberPerms = ['Group:Read:KurinWide', 'Member:Read:KurinWide'];

  beforeEach(() => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['getAuthStateValue']);
    authService.getAuthStateValue.and.returnValue(null);

    TestBed.configureTestingModule({
      providers: [
        PermissionService,
        { provide: AuthService, useValue: authService }
      ]
    });

    service = TestBed.inject(PermissionService);
  });

  it('treats a Зв\'язковий (steward) as a whole-kurin manager', () => {
    setState(stewardPerms);
    expect(service.isManager()).toBeTrue();
    expect(service.canManageGroups()).toBeTrue();
    expect(service.canManageKurinSettings()).toBeTrue();
    expect(service.canSetupLeadership()).toBeTrue();
    expect(service.canManagePlanning()).toBeTrue();
  });

  it('treats a Курінний (lead) as a manager without kurin settings or office assignment', () => {
    setState(leadPerms);
    expect(service.isManager()).toBeTrue();
    expect(service.canManageGroups()).toBeTrue();
    expect(service.canManageKurinSettings()).toBeFalse();
    expect(service.canSetupLeadership()).toBeFalse();
  });

  it('treats a Гуртковий as a group leader but not a whole-kurin manager', () => {
    setState(groupLeadPerms);
    expect(service.isManager()).toBeFalse();
    expect(service.isMentor()).toBeTrue();
    expect(service.isReviewer()).toBeTrue();
    expect(service.canManageAgenda()).toBeTrue();
    expect(service.canManageKurinSettings()).toBeFalse();
  });

  it('treats a bare member as read-only', () => {
    setState(memberPerms);
    expect(service.isAdmin()).toBeFalse();
    expect(service.isManager()).toBeFalse();
    expect(service.isMentor()).toBeFalse();
    expect(service.isReviewer()).toBeFalse();
    expect(service.canManageAgenda()).toBeFalse();
    expect(service.canManageKurinSettings()).toBeFalse();
  });

  it('treats an admin as able to do everything', () => {
    setState([], true);
    expect(service.isAdmin()).toBeTrue();
    expect(service.isManager()).toBeTrue();
    expect(service.canManageKurinSettings()).toBeTrue();
    expect(service.canSetupLeadership()).toBeTrue();
  });

  it('defaults to no access when there is no auth state', () => {
    authService.getAuthStateValue.and.returnValue(null);
    expect(service.isAdmin()).toBeFalse();
    expect(service.isManager()).toBeFalse();
    expect(service.canManageAgenda()).toBeFalse();
  });
});
