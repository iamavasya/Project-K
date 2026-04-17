import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, ParamMap, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { AuthService } from '../../authModule/services/authService/auth.service';
import { MemberService } from '../common/services/member-service/member.service';
import { BadgesCatalogService } from '../common/services/probes-and-badges/badges-catalog.service';
import { MemberProgressService } from '../common/services/probes-and-badges/member-progress.service';
import { BadgeImageBlobService } from '../common/services/probes-and-badges/badge-image-blob.service';
import { BadgeProgressStatus } from '../common/models/enums/badge-progress-status.enum';
import { SkillsReviewPageComponent } from './skills-review-page.component';

describe('SkillsReviewPageComponent', () => {
  let fixture: ComponentFixture<SkillsReviewPageComponent>;
  let component: SkillsReviewPageComponent;

  let authServiceSpy: jasmine.SpyObj<AuthService>;
  let memberServiceSpy: jasmine.SpyObj<MemberService>;
  let badgesCatalogServiceSpy: jasmine.SpyObj<BadgesCatalogService>;
  let memberProgressServiceSpy: jasmine.SpyObj<MemberProgressService>;
  let badgeImageBlobServiceSpy: jasmine.SpyObj<BadgeImageBlobService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let paramMapSubject: BehaviorSubject<ParamMap>;

  beforeEach(async () => {
    authServiceSpy = jasmine.createSpyObj<AuthService>('AuthService', ['getAuthStateValue']);
    memberServiceSpy = jasmine.createSpyObj<MemberService>('MemberService', ['getAll']);
    badgesCatalogServiceSpy = jasmine.createSpyObj<BadgesCatalogService>('BadgesCatalogService', ['getAll']);
    memberProgressServiceSpy = jasmine.createSpyObj<MemberProgressService>('MemberProgressService', ['getBadgeProgresses', 'reviewBadgeProgress']);
    badgeImageBlobServiceSpy = jasmine.createSpyObj<BadgeImageBlobService>('BadgeImageBlobService', ['resolveBadgeImageForDisplay']);
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);
    paramMapSubject = new BehaviorSubject(convertToParamMap({ kurinKey: 'kurin-1' }));

    badgeImageBlobServiceSpy.resolveBadgeImageForDisplay.and.callFake((url: string | null) => url);

    authServiceSpy.getAuthStateValue.and.returnValue({
      userKey: 'user-1',
      email: 'mentor@example.com',
      role: 'Mentor',
      kurinKey: 'kurin-1',
      accessToken: 'token'
    });

    memberServiceSpy.getAll.and.returnValue(of([
      {
        memberKey: 'member-1',
        groupKey: 'group-1',
        kurinKey: 'kurin-1',
        firstName: 'Іван',
        middleName: '',
        lastName: 'Петренко',
        email: 'm1@example.com',
        phoneNumber: '0990000001',
        dateOfBirth: null,
        profilePhotoUrl: null,
        plastLevelHistories: [],
        leadershipHistories: []
      },
      {
        memberKey: 'member-2',
        groupKey: 'group-1',
        kurinKey: 'kurin-1',
        firstName: 'Марія',
        middleName: '',
        lastName: 'Коваль',
        email: 'm2@example.com',
        phoneNumber: '0990000002',
        dateOfBirth: null,
        profilePhotoUrl: null,
        plastLevelHistories: [],
        leadershipHistories: []
      }
    ]));

    badgesCatalogServiceSpy.getAll.and.returnValue(of([
      {
        id: 'badge-1',
        title: 'Вузли',
        imagePath: 'knots.jpg',
        country: 'UA',
        specialization: 'Scout',
        status: 'Active',
        level: 1,
        lastUpdated: '2026-04-16T00:00:00Z',
        seekerRequirements: '',
        instructorRequirements: '',
        fixNotes: []
      }
    ]));

    memberProgressServiceSpy.getBadgeProgresses.and.callFake((memberKey: string) => {
      if (memberKey === 'member-1') {
        return of([
          {
            badgeProgressKey: 'bp-1',
            memberKey,
            kurinKey: 'kurin-1',
            badgeId: 'badge-1',
            status: BadgeProgressStatus.Submitted,
            submittedAtUtc: '2026-04-17T10:00:00Z',
            reviewedAtUtc: null,
            reviewedByUserKey: null,
            reviewedByName: null,
            reviewedByRole: null,
            reviewNote: null,
            auditTrail: []
          }
        ]);
      }

      return of([
        {
          badgeProgressKey: 'bp-2',
          memberKey,
          kurinKey: 'kurin-1',
          badgeId: 'badge-1',
          status: BadgeProgressStatus.Confirmed,
          submittedAtUtc: '2026-04-15T10:00:00Z',
          reviewedAtUtc: '2026-04-16T10:00:00Z',
          reviewedByUserKey: 'reviewer-1',
          reviewedByName: 'Mentor',
          reviewedByRole: 'Mentor',
          reviewNote: null,
          auditTrail: []
        }
      ]);
    });

    memberProgressServiceSpy.reviewBadgeProgress.and.returnValue(of({
      badgeProgressKey: 'bp-1',
      memberKey: 'member-1',
      kurinKey: 'kurin-1',
      badgeId: 'badge-1',
      status: BadgeProgressStatus.Confirmed,
      submittedAtUtc: '2026-04-17T10:00:00Z',
      reviewedAtUtc: '2026-04-17T11:00:00Z',
      reviewedByUserKey: 'reviewer-1',
      reviewedByName: 'Mentor',
      reviewedByRole: 'Mentor',
      reviewNote: null,
      auditTrail: []
    }));

    await TestBed.configureTestingModule({
      imports: [SkillsReviewPageComponent],
      providers: [
        { provide: AuthService, useValue: authServiceSpy },
        { provide: MemberService, useValue: memberServiceSpy },
        { provide: BadgesCatalogService, useValue: badgesCatalogServiceSpy },
        { provide: MemberProgressService, useValue: memberProgressServiceSpy },
        { provide: BadgeImageBlobService, useValue: badgeImageBlobServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: { paramMap: paramMapSubject.asObservable() } }
      ]
    }).compileComponents();
  });

  function createComponent() {
    fixture = TestBed.createComponent(SkillsReviewPageComponent);
    component = fixture.componentInstance;
  }

  it('should create and load pending skills queue', () => {
    createComponent();
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(component.reviewItems.length).toBe(1);
    expect(component.reviewItems[0].badgeTitle).toBe('Вузли');
    expect(component.reviewItems[0].memberDisplayName).toBe('Іван Петренко');
  });

  it('should open review dialog and approve a pending skill from dialog', () => {
    createComponent();
    fixture.detectChanges();

    const reviewItem = component.reviewItems[0];
    component.onReview(reviewItem, true);

    expect(component.isReviewDialogVisible).toBeTrue();
    expect(component.pendingReviewItem?.reviewKey).toBe(reviewItem.reviewKey);
    expect(component.pendingReviewIsApproved).toBeTrue();

    component.pendingReviewNote = '  '; // should be trimmed to null
    component.confirmReviewDialog();

    expect(memberProgressServiceSpy.reviewBadgeProgress).toHaveBeenCalledWith('member-1', 'badge-1', {
      isApproved: true,
      note: null
    });
    expect(component.reviewItems.length).toBe(0);
  });

  it('should send note text from dialog when rejecting skill', () => {
    createComponent();
    fixture.detectChanges();

    const reviewItem = component.reviewItems[0];
    component.onReview(reviewItem, false);
    component.pendingReviewNote = 'Потрібно допрацювати';
    component.confirmReviewDialog();

    expect(memberProgressServiceSpy.reviewBadgeProgress).toHaveBeenCalledWith('member-1', 'badge-1', {
      isApproved: false,
      note: 'Потрібно допрацювати'
    });
  });

  it('should block queue loading for non-reviewer role', () => {
    authServiceSpy.getAuthStateValue.and.returnValue({
      userKey: 'user-2',
      email: 'member@example.com',
      role: 'User',
      kurinKey: 'kurin-1',
      accessToken: 'token'
    });

    createComponent();
    fixture.detectChanges();

    expect(component.roleRestricted).toBeTrue();
    expect(memberServiceSpy.getAll).not.toHaveBeenCalled();
    expect(component.reviewItems.length).toBe(0);
  });

  it('goBackToKurin should navigate to kurin panel', () => {
    createComponent();
    fixture.detectChanges();

    component.goBackToKurin();

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/kurin']);
  });
});