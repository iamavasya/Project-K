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

  it('hands the table the same rows until the data changes', () => {
    component.manager = { memberKey: 'manager-member', userKey: 'manager-user', firstName: 'Manager', lastName: 'Member', middleName: null, userRole: 'KV.Zvyazkovyi' };
    component.mentorRows = [];

    expect(component.kvRows).toBe(component.kvRows);
    expect(component.kvRows[0]).toBe(component.kvRows[0]);
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
    expect(component.getStatusLabel(component.kvRows[1])).toBe('Інструктор');
  });

  it('names the status by the гуртки, not by the seat', () => {
    const seatedMentor = { memberKey: 'm', userKey: 'u', firstName: 'M', lastName: 'M', middleName: null, userRole: 'KV.Vykhovnyk' };
    const seatedInstructor = { ...seatedMentor, userRole: 'KV.Instruktor' };
    const group = { groupKey: 'g1', name: 'Кицьки' } as GroupDto;

    expect(component.getStatusLabel({ mentor: seatedMentor, groups: [] })).toBe('Інструктор');
    expect(component.getStatusLabel({ mentor: seatedInstructor, groups: [group] })).toBe('Впорядник');
    expect(component.getStatusLabel({ mentor: seatedMentor, groups: [], isManager: true })).toBe("Зв'язковий");
  });

  describe('the КВ seat on save', () => {
    const manager = { memberKey: 'manager-member', firstName: 'Manager', lastName: 'Member', middleName: null };
    const person = { memberKey: 'new-member', userKey: 'new-user', firstName: 'New', lastName: 'Person', middleName: null };

    function kvWith(...seats: [string, string][]): LeadershipDto {
      return {
        leadershipKey: 'kv-1',
        type: 'kv',
        entityKey: 'kurin-1',
        startDate: '2026-01-01',
        endDate: null,
        leadershipHistories: seats.map(([role, memberKey], index) => ({
          leadershipHistoryKey: `h${index}`,
          leadershipKey: 'kv-1',
          role,
          startDate: '2026-01-01',
          endDate: null,
          member: memberKey === manager.memberKey ? manager : person
        }))
      };
    }

    function save(leadership: LeadershipDto, groupKeys: string[]): void {
      leadershipService.getLeadershipByTypeAndKey.and.returnValue(of(leadership));
      leadershipService.update.and.returnValue(of(leadership));
      groupService.assignMentor.and.returnValue(of(void 0) as never);
      groupService.getAllByKurinKey.and.returnValue(of([]));
      groupService.getMentorAssignments.and.returnValue(of([]));
      memberService.getKVMembers.and.returnValue(of([]));
      memberService.getMentorCandidates.and.returnValue(of([]));
      fixture.componentRef.setInput('kurinKey', 'kurin-1');
      component.mentorCandidates = [person];
      component.selectedMentorUserKey = 'new-user';
      component.selectedGroupKeys = groupKeys;

      component.saveAssignments();
    }

    const sentSeats = () => leadershipService.update.calls.mostRecent().args[1].leadershipHistories
      .map(history => [history.role, history.member.memberKey]);

    it('seats someone saved without a гурток as інструктор', () => {
      save(kvWith(['Zvyazkovyi', 'manager-member']), []);

      expect(leadershipService.update.calls.mostRecent().args[0]).toBe('kv-1');
      expect(sentSeats()).toEqual([['Zvyazkovyi', 'manager-member'], ['Instruktor', 'new-member']]);
      // The API binds the keys as Guid?: an empty string is a 400, a new seat goes with null.
      const seat = leadershipService.update.calls.mostRecent().args[1].leadershipHistories[1];
      expect(seat.leadershipHistoryKey).toBeNull();
      expect(seat.leadershipKey).toBe('kv-1');
    });

    it('turns a впорядник left without гуртки into an інструктор', () => {
      save(kvWith(['Zvyazkovyi', 'manager-member'], ['Vykhovnyk', 'new-member']), []);

      expect(sentSeats()).toEqual([['Zvyazkovyi', 'manager-member'], ['Instruktor', 'new-member']]);
    });

    it('ends the інструктор seat once a гурток is assigned', () => {
      save(kvWith(['Zvyazkovyi', 'manager-member'], ['Instruktor', 'new-member']), ['g1']);

      expect(groupService.assignMentor).toHaveBeenCalledWith('g1', 'new-user');
      expect(sentSeats()).toEqual([['Zvyazkovyi', 'manager-member']]);
    });

    it('leaves the провід alone for a newcomer given a гурток', () => {
      save(kvWith(['Zvyazkovyi', 'manager-member']), ['g1']);

      expect(groupService.assignMentor).toHaveBeenCalledWith('g1', 'new-user');
      expect(leadershipService.update).not.toHaveBeenCalled();
    });
  });
});

