import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { UserService } from '../../../adminModule/services/user-service/user.service';
import { AuthService } from '../../../authModule/services/auth-service/auth.service';
import { PermissionService } from '../../../authModule/services/permission-service/permission.service';
import { GroupService } from '../../services/group-service/group.service';
import { MemberService } from '../../services/member-service/member.service';
import { KvPanelComponent } from './kv-panel';
import { GroupDto } from '../../models/group.dto';
import { LeadershipService } from '../../services/leadership-service/leadership.service';
import { LeadershipDto } from '../../models/requests/leadership/leadership.dto';

describe('KvPanelComponent', () => {
  let component: KvPanelComponent;
  let fixture: ComponentFixture<KvPanelComponent>;
  let router: jasmine.SpyObj<Router>;
  let groupService: jasmine.SpyObj<GroupService>;
  let memberService: jasmine.SpyObj<MemberService>;
  let leadershipService: jasmine.SpyObj<LeadershipService>;

  beforeEach(async () => {
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);
    groupService = jasmine.createSpyObj<GroupService>('GroupService', [
      'getAllByKurinKey',
      'getMentorAssignments',
      'assignMentor',
      'revokeMentor'
    ]);
    memberService = jasmine.createSpyObj<MemberService>('MemberService', [
      'getKVMembers',
      'getMentorCandidates'
    ]);
    leadershipService = jasmine.createSpyObj<LeadershipService>('LeadershipService', [
      'getLeadershipByTypeAndKey',
      'update',
      'create'
    ]);

    await TestBed.configureTestingModule({
      imports: [KvPanelComponent],
      providers: [
        { provide: GroupService, useValue: groupService },
        { provide: MemberService, useValue: memberService },
        { provide: LeadershipService, useValue: leadershipService },
        {
          provide: PermissionService,
          useValue: {
            canManageMentors: () => true,
            isManager: () => true
          }
        },
        {
          provide: AuthService,
          useValue: {
            getAuthStateValue: () => null,
            updatePermissions: jasmine.createSpy('updatePermissions'),
            refreshToken: () => of(null)
          }
        },
        {
          provide: UserService,
          useValue: jasmine.createSpyObj<UserService>('UserService', ['changeUserRole'])
        },
        { provide: Router, useValue: router }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(KvPanelComponent);
    component = fixture.componentInstance;
  });

  it('should place the manager before mentor rows', () => {
    component.manager = {
      memberKey: 'manager-member',
      firstName: 'Manager',
      lastName: 'Member',
      middleName: null,
      userRole: 'Manager'
    };
    component.mentorRows = [{
      mentor: {
        memberKey: 'mentor-member',
        firstName: 'Mentor',
        lastName: 'Member',
        middleName: null,
        userRole: 'Mentor'
      },
      groups: []
    }];

    expect(component.kvRows.map(row => row.mentor.memberKey))
      .toEqual(['manager-member', 'mentor-member']);
    expect(component.kvRows[0].isManager).toBeTrue();
  });

  it('shows the Звʼязковий once, with the гуртки they are actually assigned to', () => {
    const manager = { memberKey: 'manager-member', userKey: 'manager-user', firstName: 'Manager', lastName: 'Member', middleName: null, userRole: 'Manager' };
    const group = { groupKey: 'g1', name: 'Кицьки' } as GroupDto;
    component.manager = manager;
    component.mentorRows = [
      { mentor: { ...manager }, groups: [group] },
      { mentor: { memberKey: 'mentor-member', userKey: 'mentor-user', firstName: 'Mentor', lastName: 'Member', middleName: null, userRole: 'Mentor' }, groups: [] }
    ];

    expect(component.kvRows.map(row => row.mentor.memberKey)).toEqual(['manager-member', 'mentor-member']);
    expect(component.kvRows[0].groups).toEqual([group]);
  });

  it('should navigate to the selected member profile', () => {
    component.onMemberSelect({
      memberKey: 'member-1',
      firstName: 'Test',
      lastName: 'Member',
      middleName: null
    });

    expect(router.navigate).toHaveBeenCalledWith(['/member', 'member-1']);
  });

  it('gives an Інструктор a row of their own', () => {
    groupService.getAllByKurinKey.and.returnValue(of([]));
    groupService.getMentorAssignments.and.returnValue(of([]));
    memberService.getKVMembers.and.returnValue(of([
      { memberKey: 'manager-member', userKey: 'manager-user', firstName: 'Manager', lastName: 'Member', middleName: null, userRole: 'KV.Zvyazkovyi' },
      { memberKey: 'instructor-member', userKey: 'instructor-user', firstName: 'Ira', lastName: 'Instructor', middleName: null, userRole: 'KV.Instruktor' }
    ]));
    memberService.getMentorCandidates.and.returnValue(of([]));
    fixture.componentRef.setInput('kurinKey', 'kurin-1');

    component.loadData();

    expect(component.kvRows.map(row => row.mentor.memberKey)).toEqual(['manager-member', 'instructor-member']);
    expect(component.getStatusLabel(component.kvRows[1].mentor)).toBe('Інструктор');
  });

  it('seats a впорядник without a гурток when saved with none', () => {
    const leadership: LeadershipDto = {
      leadershipKey: 'kv-1',
      type: 'kv',
      entityKey: 'kurin-1',
      startDate: '2026-01-01',
      endDate: null,
      leadershipHistories: [{
        leadershipHistoryKey: 'h1',
        leadershipKey: 'kv-1',
        role: 'Zvyazkovyi',
        startDate: '2026-01-01',
        endDate: null,
        member: { memberKey: 'manager-member', firstName: 'Manager', lastName: 'Member', middleName: null }
      }]
    };
    leadershipService.getLeadershipByTypeAndKey.and.returnValue(of(leadership));
    leadershipService.update.and.returnValue(of(leadership));
    groupService.getAllByKurinKey.and.returnValue(of([]));
    groupService.getMentorAssignments.and.returnValue(of([]));
    memberService.getKVMembers.and.returnValue(of([]));
    memberService.getMentorCandidates.and.returnValue(of([]));
    fixture.componentRef.setInput('kurinKey', 'kurin-1');
    component.mentorCandidates = [{ memberKey: 'new-member', userKey: 'new-user', firstName: 'New', lastName: 'Mentor', middleName: null }];
    component.selectedMentorUserKey = 'new-user';
    component.selectedGroupKeys = [];

    component.saveAssignments();

    const [key, payload] = leadershipService.update.calls.mostRecent().args;
    expect(key).toBe('kv-1');
    expect(payload.leadershipHistories.map(history => [history.role, history.member.memberKey]))
      .toEqual([['Zvyazkovyi', 'manager-member'], ['Vykhovnyk', 'new-member']]);
  });

  it('opens the КВ провід form for its leadership', () => {
    leadershipService.getLeadershipByTypeAndKey.and.returnValue(of({ leadershipKey: 'kv-1', startDate: '', endDate: null, leadershipHistories: [] }));
    fixture.componentRef.setInput('kurinKey', 'kurin-1');

    component.openKvLeadership();

    expect(router.navigate).toHaveBeenCalledWith(['/leadership', 'kv-1', 'kv', 'kurin-1']);
  });
});

