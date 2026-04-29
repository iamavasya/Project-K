import { ComponentFixture, TestBed } from '@angular/core/testing';
import { GroupPanelComponent } from './group-panel.component';
import { ActivatedRoute, convertToParamMap, ParamMap, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { MemberService } from '../common/services/member-service/member.service';
import { GroupService } from '../common/services/group-service/group.service';
import { LeadershipService } from '../common/services/leadership-service/leadership-service';
import { GroupDto } from '../common/models/groupDto';
import { LeadershipDto } from '../common/models/requests/leadership/leadershipDto';
import { MemberDto } from '../common/models/memberDto';
import { AuthService } from '../../authModule/services/authService/auth.service';

describe('GroupPanelComponent', () => {
  let fixture: ComponentFixture<GroupPanelComponent>;
  let component: GroupPanelComponent;

  let memberServiceSpy: jasmine.SpyObj<MemberService>;
  let groupServiceSpy: jasmine.SpyObj<GroupService>;
  let leadershipServiceSpy: jasmine.SpyObj<LeadershipService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let paramMapSubject: BehaviorSubject<ParamMap>;

  const groupKey = 'group1';
  const group: GroupDto = {
    groupKey,
    name: 'Test Group',
    kurinKey: 'kurin1',
    kurinNumber: 1
  };

  const leadership: LeadershipDto = {
    leadershipKey: 'l1',
    startDate: '2026-01-01',
    endDate: null,
    leadershipHistories: []
  };

  beforeEach(async () => {
    memberServiceSpy = jasmine.createSpyObj('MemberService', ['getAll', 'getMentorCandidates']);
    groupServiceSpy = jasmine.createSpyObj('GroupService', ['getByKey', 'exists', 'getMentors', 'assignMentor', 'revokeMentor']);
    leadershipServiceSpy = jasmine.createSpyObj('LeadershipService', ['getLeadershipByTypeAndKey']);
    authServiceSpy = jasmine.createSpyObj('AuthService', ['getAuthStateValue']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);
    paramMapSubject = new BehaviorSubject(convertToParamMap({ groupKey }));

    groupServiceSpy.exists.and.returnValue(of(true));
    groupServiceSpy.getByKey.and.returnValue(of(group));
    groupServiceSpy.getMentors.and.returnValue(of([]));
    groupServiceSpy.assignMentor.and.returnValue(of({}));
    groupServiceSpy.revokeMentor.and.returnValue(of({}));
    memberServiceSpy.getAll.and.returnValue(of([]));
    memberServiceSpy.getMentorCandidates.and.returnValue(of([]));
    leadershipServiceSpy.getLeadershipByTypeAndKey.and.returnValue(of(leadership));
    authServiceSpy.getAuthStateValue.and.returnValue({ role: 'Manager' } as never);

    await TestBed.configureTestingModule({
      imports: [GroupPanelComponent],
      providers: [
        { provide: MemberService, useValue: memberServiceSpy },
        { provide: GroupService, useValue: groupServiceSpy },
        { provide: LeadershipService, useValue: leadershipServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: { paramMap: paramMapSubject.asObservable() } }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(GroupPanelComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should load group details on init', () => {
    expect(groupServiceSpy.getByKey).toHaveBeenCalledWith(groupKey);
    expect(component.group).toEqual(group);
  });

  it('should navigate to panel if group does not exist', () => {
    groupServiceSpy.exists.and.returnValue(of(false));
    component.refreshData();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/panel'], { replaceUrl: true });
  });

  it('onMemberCreate should navigate to upsert route', () => {
    component.onMemberCreate();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/group', groupKey, 'member', 'upsert']);
  });

  it('onMemberSelect should navigate to member card', () => {
    component.selectedMember = { memberKey: 'm1' } as unknown as MemberDto;
    component.onMemberSelect();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/member', 'm1']);
  });

  it('openMentorDialog should load management data and open dialog', () => {
    component.openMentorDialog();

    expect(memberServiceSpy.getMentorCandidates).toHaveBeenCalledWith(group.kurinKey);
    expect(groupServiceSpy.getMentors).toHaveBeenCalledWith(groupKey);
    expect(component.mentorDialogVisible).toBeTrue();
  });

  it('saveMentorAssignments should call assign and revoke based on diff', () => {
    component.initialMentorUserKeys = ['u1', 'u2'];
    component.selectedMentorUserKeys = ['u2', 'u3'];
    component.mentorDialogVisible = true;

    component.saveMentorAssignments();

    expect(groupServiceSpy.assignMentor).toHaveBeenCalledWith(groupKey, 'u3');
    expect(groupServiceSpy.revokeMentor).toHaveBeenCalledWith(groupKey, 'u1');
    expect(component.mentorDialogVisible).toBeFalse();
  });
});
