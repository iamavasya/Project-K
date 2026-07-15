import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { UserService } from '../../../../adminModule/services/user.service';
import { AuthService } from '../../../../authModule/services/authService/auth.service';
import { PermissionService } from '../../../../authModule/services/permission.service';
import { GroupService } from '../../services/group-service/group.service';
import { MemberService } from '../../services/member-service/member.service';
import { KvPanelComponent } from './kv-panel';

describe('KvPanelComponent', () => {
  let component: KvPanelComponent;
  let router: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    router = jasmine.createSpyObj<Router>('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [KvPanelComponent],
      providers: [
        {
          provide: GroupService,
          useValue: jasmine.createSpyObj<GroupService>('GroupService', [
            'getAllByKurinKey',
            'getMentorAssignments',
            'assignMentor',
            'revokeMentor'
          ])
        },
        {
          provide: MemberService,
          useValue: jasmine.createSpyObj<MemberService>('MemberService', [
            'getKVMembers',
            'getMentorCandidates'
          ])
        },
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
            updateRole: jasmine.createSpy('updateRole'),
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

    component = TestBed.createComponent(KvPanelComponent).componentInstance;
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

  it('should navigate to the selected member profile', () => {
    component.onMemberSelect({
      memberKey: 'member-1',
      firstName: 'Test',
      lastName: 'Member',
      middleName: null
    });

    expect(router.navigate).toHaveBeenCalledWith(['/member', 'member-1']);
  });
});
