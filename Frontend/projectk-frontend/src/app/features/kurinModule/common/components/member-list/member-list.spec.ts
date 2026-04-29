import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MemberList } from './member-list';
import { MemberService } from '../../services/member-service/member.service';
import { LeadershipService } from '../../services/leadership-service/leadership-service';
import { Router } from '@angular/router';
import { of } from 'rxjs';
import { MemberDto } from '../../models/memberDto';
import { LeadershipDto, LeadershipHistoryDto } from '../../models/requests/leadership/leadershipDto';
import { LeadershipRole } from '../../models/enums/leadership-role.enum';
import { AuthService } from '../../../../authModule/services/authService/auth.service';

describe('MemberList', () => {
  let component: MemberList;
  let fixture: ComponentFixture<MemberList>;
  let memberServiceSpy: jasmine.SpyObj<MemberService>;
  let leadershipServiceSpy: jasmine.SpyObj<LeadershipService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;

  const mockMembers: MemberDto[] = [
    { memberKey: 'm1', firstName: 'John', lastName: 'Doe', middleName: 'M' } as MemberDto,
  ];

  const mockLeadership: LeadershipDto = {
    leadershipKey: 'l1',
    type: 'kurin',
    entityKey: 'k1',
    startDate: '2023-01-01',
    endDate: null,
    leadershipHistories: [
      {
        leadershipKey: 'l1',
        leadershipHistoryKey: 'lh1',
        role: LeadershipRole.Kurinnuy,
        startDate: '2023-01-01',
        endDate: null, // Active
        member: { memberKey: 'm1', firstName: 'John', lastName: 'Doe', middleName: 'M' }
      },
      {
        leadershipKey: 'l1',
        leadershipHistoryKey: 'lh2',
        role: LeadershipRole.Pysar,
        startDate: '2022-01-01',
        endDate: '2023-01-01', // Archived
        member: { memberKey: 'm2', firstName: 'Jane', lastName: 'Smith', middleName: 'A' }
      }
    ]
  };

  beforeEach(async () => {
    memberServiceSpy = jasmine.createSpyObj('MemberService', ['getAll']);
    leadershipServiceSpy = jasmine.createSpyObj('LeadershipService', ['getLeadershipByTypeAndKey']);
    authServiceSpy = jasmine.createSpyObj('AuthService', ['getAuthStateValue']);
    routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    authServiceSpy.getAuthStateValue.and.returnValue({ role: 'Manager' } as any);

    await TestBed.configureTestingModule({
      imports: [MemberList],
      providers: [
        { provide: MemberService, useValue: memberServiceSpy },
        { provide: LeadershipService, useValue: leadershipServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(MemberList);
    component = fixture.componentInstance;
    
    // Default mocks
    memberServiceSpy.getAll.and.returnValue(of(mockMembers));
    leadershipServiceSpy.getLeadershipByTypeAndKey.and.returnValue(of(mockLeadership));
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('ngOnInit', () => {
    it('should do nothing if typeKey is missing', () => {
      component.typeKey = '';
      component.ngOnInit();
      expect(memberServiceSpy.getAll).not.toHaveBeenCalled();
      expect(leadershipServiceSpy.getLeadershipByTypeAndKey).not.toHaveBeenCalled();
    });

    it('should load members for group type', () => {
      component.type = 'group';
      component.typeKey = 'g1';
      component.ngOnInit();
      expect(memberServiceSpy.getAll).toHaveBeenCalledWith('g1');
      expect(component.membersLookup.length).toBe(1);
      expect(component.membersLookup[0].phoneNumber).toBeUndefined();
    });

    it('should load members for kurin type', () => {
      component.type = 'kurin';
      component.typeKey = 'k1';
      component.ngOnInit();
      expect(memberServiceSpy.getAll).toHaveBeenCalledWith(undefined, 'k1');
      expect(component.membersLookup.length).toBe(1);
    });

    it('should load leadership for leadership type', () => {
      component.type = 'leadership';
      component.leadershipType = 'kurin';
      component.typeKey = 'k1';
      component.ngOnInit();
      expect(leadershipServiceSpy.getLeadershipByTypeAndKey).toHaveBeenCalledWith('kurin', 'k1');
      expect(component.leadership).toEqual(mockLeadership);
      expect(component.allHistories.length).toBe(2);
    });
  });

  describe('Leadership List Logic', () => {
    beforeEach(() => {
      component.type = 'leadership';
      component.typeKey = 'k1';
      component.ngOnInit(); // Loads mockLeadership
    });

    it('should filter archived rows by default', () => {
      expect(component.showArchived).toBeFalse();
      expect(component.leadershipHistories.length).toBe(1);
      expect(component.leadershipHistories[0].role).toBe(LeadershipRole.Kurinnuy);
    });

    it('should show all rows when showArchived is true', () => {
      component.showArchived = true;
      component.refreshList();
      expect(component.leadershipHistories.length).toBe(2);
    });

    it('should sort active rows first', () => {
      component.showArchived = true;
      component.refreshList();
      const rows = component.leadershipHistories;
      expect(rows[0].endDate).toBeFalsy(); // Active
      expect(rows[1].endDate).toBeTruthy(); // Archived
    });
  });

  describe('Navigation', () => {
    it('onMemberSelect should navigate to member profile', () => {
      const member = { memberKey: 'm1' } as MemberDto;
      component.onMemberSelect(member);
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/member', 'm1']);
    });

    it('onLeadershipSettingsSelect should navigate to edit if leadership exists', () => {
      component.leadership = mockLeadership;
      component.leadershipType = 'kurin';
      component.typeKey = 'k1';
      
      component.onLeadershipSettingsSelect();
      
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/leadership', 'l1', 'kurin', 'k1']);
    });

    it('onLeadershipSettingsSelect should navigate to create if leadership is null', () => {
      component.leadership = null;
      component.type = 'leadership'; // Ensure context is correct
      component.leadershipType = 'kurin';
      component.typeKey = 'k1';

      component.onLeadershipSettingsSelect();

      expect(routerSpy.navigate).toHaveBeenCalledWith(['/leadership/create', 'kurin', 'k1']);
    });
  });

  describe('Helpers', () => {
    it('getRoleSeverity should return secondary for archived', () => {
      const history = { endDate: '2023-01-01' } as LeadershipHistoryDto;
      expect(component.getRoleSeverity(history)).toBe('secondary');
    });

    it('getRoleSeverity should return danger for Kurinnuy', () => {
      const history = { role: LeadershipRole.Kurinnuy, endDate: null } as LeadershipHistoryDto;
      expect(component.getRoleSeverity(history)).toBe('danger');
    });

    it('getRoleSeverity should return info for unknown roles', () => {
      const history = { role: LeadershipRole.Pysar, endDate: null } as LeadershipHistoryDto;
      expect(component.getRoleSeverity(history)).toBe('info');
    });
    
    it('getRoleDisplayName should return role name', () => {
        const role = LeadershipRole.Kurinnuy;
        expect(component.getRoleDisplayName(role)).toBeTruthy();
    });
  });

  describe('Group card mode', () => {
    beforeEach(() => {
      component.type = 'group';
      component.typeKey = 'g1';
      memberServiceSpy.getAll.and.returnValue(of([
        {
          memberKey: 'm1',
          firstName: 'John',
          lastName: 'Doe',
          middleName: 'M',
          phoneNumber: '111',
          latestPlastLevelDisplay: 'пл. уч.'
        } as MemberDto,
        {
          memberKey: 'm2',
          firstName: 'Jane',
          lastName: 'Smith',
          middleName: 'A',
          phoneNumber: '222',
          latestPlastLevelDisplay: 'ст. пл. скоб'
        } as MemberDto
      ]));
      component.ngOnInit();
    });

    it('should filter members by search query', () => {
      component.memberSearchQuery = 'smith';
      expect(component.filteredMembersLookup.length).toBe(1);
      expect(component.filteredMembersLookup[0].memberKey).toBe('m2');
    });

    it('should filter by plast level localized label', () => {
      component.memberSearchQuery = 'скоб';
      expect(component.filteredMembersLookup.length).toBe(1);
      expect(component.filteredMembersLookup[0].memberKey).toBe('m2');
    });

    it('should restore card view state from session storage for current group key', () => {
      const getItemSpy = spyOn(window.sessionStorage, 'getItem').and.returnValue('true');

      component.type = 'group';
      component.typeKey = 'g42';
      component.ngOnInit();

      expect(getItemSpy).toHaveBeenCalledWith('member-list:group-card-view:g42');
      expect(component.showGroupCardView).toBeTrue();
    });

    it('should persist card view state to session storage on toggle change', () => {
      const setItemSpy = spyOn(window.sessionStorage, 'setItem');

      component.type = 'group';
      component.typeKey = 'g77';
      component.showGroupCardView = true;
      component.onGroupCardViewToggleChange();

      expect(setItemSpy).toHaveBeenCalledWith('member-list:group-card-view:g77', 'true');
    });

    it('should set hasUpcomingBirthdays to false when no birthdays within 30 days', () => {
      memberServiceSpy.getAll.and.returnValue(of([
        {
          memberKey: 'm1',
          groupKey: 'g1',
          kurinKey: 'k1',
          firstName: 'John',
          lastName: 'Doe',
          middleName: 'M',
          email: 'john@example.com',
          phoneNumber: '123456789',
          dateOfBirth: null,
          plastLevelHistories: [],
          leadershipHistories: [],
          profilePhotoUrl: null
        } as MemberDto
      ]));

      component.type = 'group';
      component.typeKey = 'g1';
      component.ngOnInit();

      expect(component.hasUpcomingBirthdays).toBeFalse();
    });
  });
});
