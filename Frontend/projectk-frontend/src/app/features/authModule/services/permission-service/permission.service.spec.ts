import { TestBed } from '@angular/core/testing';
import { PermissionService } from './permission.service';
import { AuthService } from '../auth-service/auth.service';
import { AuthState } from '../../models/auth-state.model';

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
  const vykhovnykPerms = ['Group:Update:OwnGroups', 'Member:Update:OwnGroups', 'AgendaItem:Create:KurinWide', 'PlanningSession:Create:OwnGroups'];
  const providPerms = ['AgendaItem:Create:KurinWide', 'PlanningSession:Create:KurinWide'];
  const kurinnyyPerms = [...providPerms, 'Leadership:Update:KurinWide'];
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

  // Курінну касу бачать лише ті, кому вона дана грантом: скарбник куреня і Звʼязковий.
  it('shows the kurin box to its keepers only', () => {
    setState([...kurinnyyPerms, 'KurinDues:Read:KurinWide', 'KurinDues:Update:KurinWide']);
    expect(service.canSeeKurinDues()).toBeTrue();

    setState([...memberPerms, 'GroupDues:Read:Own']);
    expect(service.canSeeKurinDues()).toBeFalse();

    setState(vykhovnykPerms);
    expect(service.canSeeKurinDues()).toBeFalse();
  });

  // A суддя scores; a youth with only their own points does not, and only the kurin's суддя sets the rules.
  it('tells who scores and who sets the rules of точкування', () => {
    setState([...memberPerms, 'GroupScore:Create:OwnGroups', 'GroupScore:Read:OwnGroups']);
    expect(service.canScore()).toBeTrue();
    expect(service.canManageScore()).toBeFalse();

    setState([...kurinnyyPerms, 'GroupScore:Create:KurinWide', 'KurinScore:Manage:KurinWide']);
    expect(service.canManageScore()).toBeTrue();

    setState([...memberPerms, 'GroupScore:Read:Own']);
    expect(service.canScore()).toBeFalse();
  });

  it('shows a гурток box to its keepers, not to a youth with only their own balance', () => {
    setState([...vykhovnykPerms, 'GroupDues:Read:OwnGroups']);
    expect(service.canSeeGroupDues()).toBeTrue();

    setState([...memberPerms, 'GroupDues:Read:Own']);
    expect(service.canSeeGroupDues()).toBeFalse();
  });

  it('treats a Зв\'язковий (steward) as a whole-kurin manager', () => {
    setState(stewardPerms);
    expect(service.canManageWholeKurin()).toBeTrue();
    expect(service.canManageGroups()).toBeTrue();
    expect(service.canManageKurinSettings()).toBeTrue();
    expect(service.canSetupLeadership('kv')).toBeTrue();
    expect(service.canSetupLeadership('group')).toBeTrue();
  });

  it('treats a Курінний (lead) as a manager without kurin settings or office assignment', () => {
    setState(leadPerms);
    expect(service.canManageWholeKurin()).toBeTrue();
    expect(service.canManageGroups()).toBeTrue();
    expect(service.canManageKurinSettings()).toBeFalse();
    expect(service.canSetupLeadership('kurin')).toBeFalse();
  });

  it('treats a Виховник as a group leader but not a whole-kurin manager', () => {
    setState(vykhovnykPerms);
    expect(service.canManageWholeKurin()).toBeFalse();
    expect(service.canLeadGroups()).toBeTrue();
    expect(service.isReviewer()).toBeTrue();
    expect(service.canManageAgenda()).toBeTrue();
    expect(service.canManageKurinSettings()).toBeFalse();
  });

  it('lets a провід office raise agenda and planning without touching groups', () => {
    setState(providPerms);
    expect(service.canManageWholeKurin()).toBeFalse();
    expect(service.canLeadGroups()).toBeFalse();
    expect(service.canManageGroups()).toBeFalse();
    expect(service.canManageAgenda()).toBeTrue();
    expect(service.canCreatePlanning()).toBeTrue();
    expect(service.canSetupLeadership('group')).toBeFalse();
  });

  it('lets a Курінний seat offices but not moderate members', () => {
    setState(kurinnyyPerms);
    expect(service.canSetupLeadership('kurin')).toBeTrue();
    expect(service.canSetupLeadership('group')).toBeFalse();
    expect(service.canSetupLeadership('kv')).toBeFalse();
    expect(service.canLeadGroups()).toBeFalse();
    expect(service.canManageMembers()).toBeFalse();
  });

  it('treats a bare member as read-only', () => {
    setState(memberPerms);
    expect(service.isAdmin()).toBeFalse();
    expect(service.canManageWholeKurin()).toBeFalse();
    expect(service.canLeadGroups()).toBeFalse();
    expect(service.isReviewer()).toBeFalse();
    expect(service.canManageAgenda()).toBeFalse();
    expect(service.canManageKurinSettings()).toBeFalse();
  });

  it('treats an admin as able to do everything', () => {
    setState([], true);
    expect(service.isAdmin()).toBeTrue();
    expect(service.canManageWholeKurin()).toBeTrue();
    expect(service.canManageKurinSettings()).toBeTrue();
    expect(service.canSetupLeadership('kv')).toBeTrue();
  });

  // A Гуртковий seats his own гурток's провід and nothing above it: the kurin провід form is not his.
  it('lets a Гуртковий seat only his group провід', () => {
    setState([...providPerms, 'Leadership:Update:OwnGroups']);
    expect(service.canSetupLeadership('group')).toBeTrue();
    expect(service.canSetupLeadership('kurin')).toBeFalse();
    expect(service.canSetupLeadership('kv')).toBeFalse();
  });

  it('defaults to no access when there is no auth state', () => {
    authService.getAuthStateValue.and.returnValue(null);
    expect(service.isAdmin()).toBeFalse();
    expect(service.canManageWholeKurin()).toBeFalse();
    expect(service.canManageAgenda()).toBeFalse();
  });
});
