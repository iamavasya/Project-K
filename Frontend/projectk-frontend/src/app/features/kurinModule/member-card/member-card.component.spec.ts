import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MemberCardComponent } from './member-card.component';
import { ActivatedRoute, convertToParamMap, ParamMap, Router } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { MemberService } from '../common/services/member-service/member.service';
import { MemberDto } from '../common/models/memberDto';
import { BadgesCatalogService } from '../common/services/probes-and-badges/badges-catalog.service';
import { ProbesCatalogService } from '../common/services/probes-and-badges/probes-catalog.service';
import { MemberProgressService } from '../common/services/probes-and-badges/member-progress.service';
import { BadgeImageBlobService } from '../common/services/probes-and-badges/badge-image-blob.service';
import { AuthService } from '../../authModule/services/authService/auth.service';
import { BadgeProgressStatus } from '../common/models/enums/badge-progress-status.enum';
import { ProbeProgressStatus } from '../common/models/enums/probe-progress-status.enum';
import { BadgeCatalogItemDto } from '../common/models/probes-and-badges/badgeCatalogItemDto';
import { BadgeProgressDto } from '../common/models/probes-and-badges/badgeProgressDto';
import { ProbeSummaryDto } from '../common/models/probes-and-badges/probeSummaryDto';

describe('MemberCardComponent', () => {
  let fixture: ComponentFixture<MemberCardComponent>;
  let component: MemberCardComponent;

  let memberServiceSpy: jasmine.SpyObj<MemberService>;
  let badgesCatalogServiceSpy: jasmine.SpyObj<BadgesCatalogService>;
  let probesCatalogServiceSpy: jasmine.SpyObj<ProbesCatalogService>;
  let memberProgressServiceSpy: jasmine.SpyObj<MemberProgressService>;
  let badgeImageBlobServiceSpy: jasmine.SpyObj<BadgeImageBlobService>;
  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let paramMapSubject: BehaviorSubject<ParamMap>;

  const memberKey = 'abc123';
  const member: MemberDto = {
    memberKey,
    groupKey: 'group1',
    kurinKey: 'kurin1',
    firstName: 'John',
    middleName: 'M',
    lastName: 'Doe',
    email: 'john@example.com',
    phoneNumber: '123456789',
    dateOfBirth: null,
    profilePhotoUrl: null,
    plastLevelHistories: [],
    leadershipHistories: []
  };

  beforeEach(async () => {
    memberServiceSpy = jasmine.createSpyObj<MemberService>('MemberService', ['getByKey']);
    badgesCatalogServiceSpy = jasmine.createSpyObj<BadgesCatalogService>('BadgesCatalogService', ['getAll']);
    probesCatalogServiceSpy = jasmine.createSpyObj<ProbesCatalogService>('ProbesCatalogService', ['getAll']);
    memberProgressServiceSpy = jasmine.createSpyObj<MemberProgressService>('MemberProgressService', ['getBadgeProgresses', 'submitBadgeProgress', 'reviewBadgeProgress', 'getProbeProgress']);
    badgeImageBlobServiceSpy = jasmine.createSpyObj<BadgeImageBlobService>('BadgeImageBlobService', ['resolveBadgeImageForDisplay']);
    authServiceSpy = jasmine.createSpyObj<AuthService>('AuthService', ['getAuthStateValue']);
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);
    paramMapSubject = new BehaviorSubject(convertToParamMap({ memberKey }));

    badgeImageBlobServiceSpy.resolveBadgeImageForDisplay.and.callFake((url: string | null) => url);
    authServiceSpy.getAuthStateValue.and.returnValue({
      userKey: 'user-mentor',
      email: 'mentor@example.com',
      role: 'Mentor',
      kurinKey: member.kurinKey,
      accessToken: 'token'
    });

    badgesCatalogServiceSpy.getAll.and.returnValue(of([]));
    probesCatalogServiceSpy.getAll.and.returnValue(of([]));
    memberProgressServiceSpy.getBadgeProgresses.and.returnValue(of([]));
    memberProgressServiceSpy.getProbeProgress.and.returnValue(of({
      probeProgressKey: null,
      memberKey,
      kurinKey: member.kurinKey,
      probeId: 'probe-1',
      status: ProbeProgressStatus.NotStarted,
      completedAtUtc: null,
      completedByUserKey: null,
      completedByName: null,
      completedByRole: null,
      verifiedAtUtc: null,
      verifiedByUserKey: null,
      verifiedByName: null,
      verifiedByRole: null,
      auditTrail: []
    }));
    memberProgressServiceSpy.submitBadgeProgress.and.returnValue(of({
      badgeProgressKey: 'new-progress',
      memberKey,
      kurinKey: member.kurinKey,
      badgeId: 'badge-new',
      status: BadgeProgressStatus.Submitted,
      submittedAtUtc: '2026-04-16T00:00:00Z',
      reviewedAtUtc: null,
      reviewedByUserKey: null,
      reviewedByName: null,
      reviewedByRole: null,
      reviewNote: null,
      auditTrail: []
    }));
    memberProgressServiceSpy.reviewBadgeProgress.and.returnValue(of({
      badgeProgressKey: 'reviewed-progress',
      memberKey,
      kurinKey: member.kurinKey,
      badgeId: 'badge-reviewed',
      status: BadgeProgressStatus.Confirmed,
      submittedAtUtc: '2026-04-16T00:00:00Z',
      reviewedAtUtc: '2026-04-16T00:10:00Z',
      reviewedByUserKey: 'reviewer-1',
      reviewedByName: 'Mentor',
      reviewedByRole: 'Mentor',
      reviewNote: null,
      auditTrail: []
    }));

    await TestBed.configureTestingModule({
      imports: [MemberCardComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: MemberService, useValue: memberServiceSpy },
        { provide: BadgesCatalogService, useValue: badgesCatalogServiceSpy },
        { provide: ProbesCatalogService, useValue: probesCatalogServiceSpy },
        { provide: MemberProgressService, useValue: memberProgressServiceSpy },
        { provide: BadgeImageBlobService, useValue: badgeImageBlobServiceSpy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: { paramMap: paramMapSubject.asObservable() } }
      ]
    }).compileComponents();
  });

  function createComponent() {
    fixture = TestBed.createComponent(MemberCardComponent);
    component = fixture.componentInstance;
  }

  it('should create', () => {
    memberServiceSpy.getByKey.and.returnValue(of(member));
    createComponent();
    fixture.detectChanges();
    expect(component).toBeTruthy();
  });

  it('should read memberKey from route params and call service', () => {
    memberServiceSpy.getByKey.and.returnValue(of(member));
    createComponent();
    fixture.detectChanges();
    expect(component.memberKey).toBe(memberKey);
    expect(memberServiceSpy.getByKey).toHaveBeenCalledWith(memberKey);
    expect(memberProgressServiceSpy.getBadgeProgresses).toHaveBeenCalledWith(memberKey);
  });

  it('should build probe rows and keep third probe disabled', () => {
    const probes: ProbeSummaryDto[] = [
      { id: 'probe-1', title: 'Перша проба', pointsCount: 10, sectionsCount: 3 },
      { id: 'probe-2', title: 'Друга проба', pointsCount: 12, sectionsCount: 4 },
      { id: 'probe-3', title: 'Третя проба', pointsCount: 14, sectionsCount: 5 }
    ];

    memberServiceSpy.getByKey.and.returnValue(of(member));
    probesCatalogServiceSpy.getAll.and.returnValue(of(probes));
    memberProgressServiceSpy.getProbeProgress.and.callFake((_mk, probeId) => of({
      probeProgressKey: null,
      memberKey,
      kurinKey: member.kurinKey,
      probeId,
      status: probeId === 'probe-1' ? ProbeProgressStatus.Completed : ProbeProgressStatus.NotStarted,
      completedAtUtc: probeId === 'probe-1' ? '2026-04-16T00:00:00Z' : null,
      completedByUserKey: null,
      completedByName: null,
      completedByRole: null,
      verifiedAtUtc: null,
      verifiedByUserKey: null,
      verifiedByName: null,
      verifiedByRole: null,
      auditTrail: []
    }));

    createComponent();
    fixture.detectChanges();

    expect(component.probeRows.map(row => row.probeId)).toEqual(['probe-1', 'probe-2', 'probe-3']);
    expect(component.probeRows[0].isCompleted).toBeTrue();
    expect(component.probeRows[2].isDisabled).toBeTrue();
    expect(component.completedProbesCount).toBe(1);
  });

  it('openProbeDetails should navigate only for enabled probe rows', () => {
    memberServiceSpy.getByKey.and.returnValue(of(member));
    createComponent();
    fixture.detectChanges();

    const enabledProbe = {
      probeId: 'probe-1',
      label: 'Перша проба',
      title: 'Перша проба',
      status: ProbeProgressStatus.InProgress,
      completedAtUtc: null,
      isCompleted: false,
      isDisabled: false,
      canOpenDetails: true,
      pointsCount: 10,
      sectionsCount: 3
    };

    const disabledProbe = {
      ...enabledProbe,
      probeId: 'probe-3',
      label: 'Третя проба',
      isDisabled: true,
      canOpenDetails: false
    };

    component.openProbeDetails(enabledProbe);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/member', memberKey, 'probe', 'probe-1']);

    component.openProbeDetails(disabledProbe);

    expect(routerSpy.navigate).toHaveBeenCalledTimes(1);
  });

  it('should set member on successful load', () => {
    memberServiceSpy.getByKey.and.returnValue(of(member));
    createComponent();
    fixture.detectChanges();
    expect(component.member).toEqual(member);
  });

  it('should navigate to panel on load error when member not preset', () => {
    memberServiceSpy.getByKey.and.returnValue(throwError(() => new Error('Not found')));
    createComponent();
    fixture.detectChanges();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/panel'], { replaceUrl: true });
  });

  it('should navigate to group page on load error when existing member with groupKey set', () => {
    memberServiceSpy.getByKey.and.returnValue(of(member));
    createComponent();
    fixture.detectChanges();
    memberServiceSpy.getByKey.and.returnValue(throwError(() => new Error('Network')));
    component.member = { ...member };
    component.refreshData();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/group', member.groupKey], { replaceUrl: true });
  });

  it('onEditMember should navigate to edit route', () => {
    memberServiceSpy.getByKey.and.returnValue(of(member));
    createComponent();
    fixture.detectChanges();
    component.onEditMember();
    expect(routerSpy.navigate).toHaveBeenCalledWith(
      ['/group', member.groupKey, 'member', 'upsert', memberKey],
      { state: { fromMember: true } }
    );
  });

  it('refreshData should use latest paramMap value if it changes', () => {
    memberServiceSpy.getByKey.and.returnValues(
      of(member),
      of({ ...member, memberKey: 'newKey' })
    );
    memberProgressServiceSpy.getBadgeProgresses.and.returnValues(of([]), of([]));
    createComponent();
    fixture.detectChanges();

    paramMapSubject.next(convertToParamMap({ memberKey: 'newKey' }));

    expect(component.memberKey).toBe('newKey');
    expect(memberServiceSpy.getByKey).toHaveBeenCalledWith('newKey');
  });

  it('should build skills summary with confirmed first and pending at the end', () => {
    const badges: BadgeCatalogItemDto[] = [
      {
        id: 'badge-1',
        title: 'Badge A',
        imagePath: 'https://example.com/a.png',
        country: 'UA',
        specialization: 'Scout',
        status: 'active',
        level: 1,
        lastUpdated: '2026-04-01',
        seekerRequirements: '',
        instructorRequirements: '',
        fixNotes: []
      },
      {
        id: 'badge-2',
        title: 'Badge B',
        imagePath: 'https://example.com/b.png',
        country: 'UA',
        specialization: 'Scout',
        status: 'active',
        level: 1,
        lastUpdated: '2026-04-01',
        seekerRequirements: '',
        instructorRequirements: '',
        fixNotes: []
      }
    ];

    const progresses: BadgeProgressDto[] = [
      {
        badgeProgressKey: 'p1',
        memberKey,
        kurinKey: member.kurinKey,
        badgeId: 'badge-1',
        status: BadgeProgressStatus.Confirmed,
        submittedAtUtc: '2026-04-01T00:00:00Z',
        reviewedAtUtc: '2026-04-10T00:00:00Z',
        reviewedByUserKey: null,
        reviewedByName: null,
        reviewedByRole: null,
        reviewNote: null,
        auditTrail: []
      },
      {
        badgeProgressKey: 'p2',
        memberKey,
        kurinKey: member.kurinKey,
        badgeId: 'badge-2',
        status: BadgeProgressStatus.Submitted,
        submittedAtUtc: '2026-04-11T00:00:00Z',
        reviewedAtUtc: null,
        reviewedByUserKey: null,
        reviewedByName: null,
        reviewedByRole: null,
        reviewNote: null,
        auditTrail: []
      }
    ];

    memberServiceSpy.getByKey.and.returnValue(of(member));
    badgesCatalogServiceSpy.getAll.and.returnValue(of(badges));
    memberProgressServiceSpy.getBadgeProgresses.and.returnValue(of(progresses));

    createComponent();
    fixture.detectChanges();

    expect(component.skillsSummary.orderedPreview.map(skill => skill.badgeId)).toEqual(['badge-1', 'badge-2']);
    expect(component.pendingSkillsCount).toBe(1);
  });

  it('openAllSkillsDialog should set dialog visibility to true', () => {
    memberServiceSpy.getByKey.and.returnValue(of(member));
    createComponent();
    fixture.detectChanges();

    component.openAllSkillsDialog();

    expect(component.isAllSkillsDialogVisible).toBeTrue();
  });

  it('openAddSkillDialog should open dialog and reset search state', () => {
    memberServiceSpy.getByKey.and.returnValue(of(member));
    createComponent();
    fixture.detectChanges();

    component.addSkillSearchTerm = 'старий пошук';
    component.addSkillVisibleCount = 99;
    component.openAddSkillDialog();

    expect(component.isAddSkillDialogVisible).toBeTrue();
    expect(component.addSkillSearchTerm).toBe('');
    expect(component.addSkillVisibleCount).toBe(component.addSkillPageSize);
  });

  it('filteredAddSkillCandidates should filter by title and reset visible count on search change', () => {
    memberServiceSpy.getByKey.and.returnValue(of(member));
    badgesCatalogServiceSpy.getAll.and.returnValue(of([
      {
        id: 'badge-1',
        title: 'Вмілість Рятівник',
        imagePath: 'badges/a.png',
        country: 'UA',
        specialization: 'Scout',
        status: 'active',
        level: 1,
        lastUpdated: '2026-04-01',
        seekerRequirements: '',
        instructorRequirements: '',
        fixNotes: []
      },
      {
        id: 'badge-2',
        title: 'Вмілість Кухар',
        imagePath: 'badges/b.png',
        country: 'UA',
        specialization: 'Craft',
        status: 'active',
        level: 1,
        lastUpdated: '2026-04-01',
        seekerRequirements: '',
        instructorRequirements: '',
        fixNotes: []
      }
    ]));

    createComponent();
    fixture.detectChanges();

    component.addSkillVisibleCount = 30;
    component.addSkillSearchTerm = 'Рят';
    component.onAddSkillSearchTermChange();

    expect(component.addSkillVisibleCount).toBe(component.addSkillPageSize);
    expect(component.filteredAddSkillCandidates.map(item => item.id)).toEqual(['badge-1']);
  });

  it('loadMoreAddSkillCandidates should increase visible results count', () => {
    memberServiceSpy.getByKey.and.returnValue(of(member));
    createComponent();
    fixture.detectChanges();

    const initial = component.addSkillVisibleCount;
    component.loadMoreAddSkillCandidates();

    expect(component.addSkillVisibleCount).toBe(initial + component.addSkillPageSize);
  });

  it('submitBadge should call API and close add-skill dialog on success', () => {
    memberServiceSpy.getByKey.and.returnValue(of(member));
    createComponent();
    fixture.detectChanges();

    component.isAddSkillDialogVisible = true;
    component.submitBadge('badge-new');

    expect(memberProgressServiceSpy.submitBadgeProgress).toHaveBeenCalledWith(memberKey, 'badge-new', { note: null });
    expect(component.isAddSkillDialogVisible).toBeFalse();
    expect(component.addSkillSuccessMessage).toContain('успішно');
  });

  it('canSubmitBadge should allow re-submission for rejected badge', () => {
    memberServiceSpy.getByKey.and.returnValue(of(member));
    memberProgressServiceSpy.getBadgeProgresses.and.returnValue(of([
      {
        badgeProgressKey: 'rejected-progress',
        memberKey,
        kurinKey: member.kurinKey,
        badgeId: 'badge-rejected',
        status: BadgeProgressStatus.Rejected,
        submittedAtUtc: '2026-04-10T00:00:00Z',
        reviewedAtUtc: '2026-04-11T00:00:00Z',
        reviewedByUserKey: 'reviewer-1',
        reviewedByName: 'Mentor',
        reviewedByRole: 'Mentor',
        reviewNote: 'needs updates',
        auditTrail: []
      }
    ]));

    createComponent();
    fixture.detectChanges();

    expect(component.canSubmitBadge('badge-rejected')).toBeTrue();
    expect(component.getSubmitBadgeButtonLabel('badge-rejected')).toBe('Подати знову');
  });

  it('openSkillsReviewFromDialog should navigate to kurin skills review route for reviewer', () => {
    memberServiceSpy.getByKey.and.returnValue(of(member));
    createComponent();
    fixture.detectChanges();

    component.isAllSkillsDialogVisible = true;
    component.openSkillsReviewFromDialog();

    expect(component.isAllSkillsDialogVisible).toBeFalse();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/kurin', member.kurinKey, 'review', 'skills']);
  });

  it('approvePendingSkill should call review API with isApproved=true', () => {
    const badges: BadgeCatalogItemDto[] = [{
      id: 'badge-1',
      title: 'Badge A',
      imagePath: 'https://example.com/a.png',
      country: 'UA',
      specialization: 'Scout',
      status: 'active',
      level: 1,
      lastUpdated: '2026-04-01',
      seekerRequirements: '',
      instructorRequirements: '',
      fixNotes: []
    }];

    const progresses: BadgeProgressDto[] = [{
      badgeProgressKey: 'p-submitted',
      memberKey,
      kurinKey: member.kurinKey,
      badgeId: 'badge-1',
      status: BadgeProgressStatus.Submitted,
      submittedAtUtc: '2026-04-11T00:00:00Z',
      reviewedAtUtc: null,
      reviewedByUserKey: null,
      reviewedByName: null,
      reviewedByRole: null,
      reviewNote: null,
      auditTrail: []
    }];

    memberServiceSpy.getByKey.and.returnValue(of(member));
    badgesCatalogServiceSpy.getAll.and.returnValue(of(badges));
    memberProgressServiceSpy.getBadgeProgresses.and.returnValue(of(progresses));

    createComponent();
    fixture.detectChanges();

    const confirmSpy = spyOn(component.confirmationService, 'confirm');

    component.approvePendingSkill(component.skillsSummary.pendingConfirmation[0]);

    expect(confirmSpy).not.toHaveBeenCalled();
    expect(memberProgressServiceSpy.reviewBadgeProgress).toHaveBeenCalledWith(memberKey, 'badge-1', {
      isApproved: true,
      note: null
    });
  });

  it('removeConfirmedSkill should call review API with isApproved=false', () => {
    const badges: BadgeCatalogItemDto[] = [{
      id: 'badge-2',
      title: 'Badge B',
      imagePath: 'https://example.com/b.png',
      country: 'UA',
      specialization: 'Scout',
      status: 'active',
      level: 1,
      lastUpdated: '2026-04-01',
      seekerRequirements: '',
      instructorRequirements: '',
      fixNotes: []
    }];

    const progresses: BadgeProgressDto[] = [{
      badgeProgressKey: 'p-confirmed',
      memberKey,
      kurinKey: member.kurinKey,
      badgeId: 'badge-2',
      status: BadgeProgressStatus.Confirmed,
      submittedAtUtc: '2026-04-10T00:00:00Z',
      reviewedAtUtc: '2026-04-11T00:00:00Z',
      reviewedByUserKey: 'reviewer-1',
      reviewedByName: 'Mentor',
      reviewedByRole: 'Mentor',
      reviewNote: null,
      auditTrail: []
    }];

    memberServiceSpy.getByKey.and.returnValue(of(member));
    badgesCatalogServiceSpy.getAll.and.returnValue(of(badges));
    memberProgressServiceSpy.getBadgeProgresses.and.returnValue(of(progresses));

    createComponent();
    fixture.detectChanges();

    const confirmSpy = spyOn(component.confirmationService, 'confirm').and.callFake((options) => {
      options.accept?.();
      return component.confirmationService;
    });

    component.removeConfirmedSkill(component.skillsSummary.recentConfirmed[0]);

    expect(confirmSpy).toHaveBeenCalled();
    expect(memberProgressServiceSpy.reviewBadgeProgress).toHaveBeenCalledWith(memberKey, 'badge-2', {
      isApproved: false,
      note: null
    });
  });
});
