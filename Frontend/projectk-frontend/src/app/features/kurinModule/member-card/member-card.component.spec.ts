import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MemberCardComponent } from './member-card.component';
import { ActivatedRoute, convertToParamMap, ParamMap, Router } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { MemberService } from '../common/services/member-service/member.service';
import { MemberDto } from '../common/models/memberDto';
import { BadgesCatalogService } from '../common/services/probes-and-badges/badges-catalog.service';
import { MemberProgressService } from '../common/services/probes-and-badges/member-progress.service';
import { BadgeProgressStatus } from '../common/models/enums/badge-progress-status.enum';
import { BadgeCatalogItemDto } from '../common/models/probes-and-badges/badgeCatalogItemDto';
import { BadgeProgressDto } from '../common/models/probes-and-badges/badgeProgressDto';

describe('MemberCardComponent', () => {
  let fixture: ComponentFixture<MemberCardComponent>;
  let component: MemberCardComponent;

  let memberServiceSpy: jasmine.SpyObj<MemberService>;
  let badgesCatalogServiceSpy: jasmine.SpyObj<BadgesCatalogService>;
  let memberProgressServiceSpy: jasmine.SpyObj<MemberProgressService>;
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
    memberProgressServiceSpy = jasmine.createSpyObj<MemberProgressService>('MemberProgressService', ['getBadgeProgresses', 'submitBadgeProgress']);
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);
    paramMapSubject = new BehaviorSubject(convertToParamMap({ memberKey }));

    badgesCatalogServiceSpy.getAll.and.returnValue(of([]));
    memberProgressServiceSpy.getBadgeProgresses.and.returnValue(of([]));
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

    await TestBed.configureTestingModule({
      imports: [MemberCardComponent, HttpClientTestingModule],
      providers: [
        { provide: MemberService, useValue: memberServiceSpy },
        { provide: BadgesCatalogService, useValue: badgesCatalogServiceSpy },
        { provide: MemberProgressService, useValue: memberProgressServiceSpy },
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
      of({ ...member, memberKey: 'newKey' }),
      of({ ...member, memberKey: 'newKey' })
    );
    memberProgressServiceSpy.getBadgeProgresses.and.returnValues(of([]), of([]), of([]));
    createComponent();
    fixture.detectChanges();
    paramMapSubject.next(convertToParamMap({ memberKey: 'newKey' }));
    component.memberKey = 'newKey'; // emulate subscription update timing
    component.refreshData();
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
});
