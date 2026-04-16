import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, ParamMap, Router } from '@angular/router';
import { BehaviorSubject, of } from 'rxjs';
import { MemberService } from '../common/services/member-service/member.service';
import { ProbesCatalogService } from '../common/services/probes-and-badges/probes-catalog.service';
import { MemberProgressService } from '../common/services/probes-and-badges/member-progress.service';
import { ProbeProgressStatus } from '../common/models/enums/probe-progress-status.enum';
import { MemberProbePageComponent } from './member-probe-page.component';

describe('MemberProbePageComponent', () => {
  let fixture: ComponentFixture<MemberProbePageComponent>;
  let component: MemberProbePageComponent;

  let memberServiceSpy: jasmine.SpyObj<MemberService>;
  let probesCatalogServiceSpy: jasmine.SpyObj<ProbesCatalogService>;
  let memberProgressServiceSpy: jasmine.SpyObj<MemberProgressService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let paramMapSubject: BehaviorSubject<ParamMap>;

  const memberKey = 'member-1';
  const probeId = 'probe-1';

  beforeEach(async () => {
    memberServiceSpy = jasmine.createSpyObj<MemberService>('MemberService', ['getByKey']);
    probesCatalogServiceSpy = jasmine.createSpyObj<ProbesCatalogService>('ProbesCatalogService', ['getGroupedById']);
    memberProgressServiceSpy = jasmine.createSpyObj<MemberProgressService>('MemberProgressService', ['getProbeProgress']);
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);
    paramMapSubject = new BehaviorSubject(convertToParamMap({ memberKey, probeId }));

    memberServiceSpy.getByKey.and.returnValue(of({
      memberKey,
      groupKey: 'group-1',
      kurinKey: 'kurin-1',
      firstName: 'John',
      middleName: 'M',
      lastName: 'Doe',
      email: 'john@example.com',
      phoneNumber: '0990000000',
      dateOfBirth: null,
      profilePhotoUrl: null,
      plastLevelHistories: [],
      leadershipHistories: []
    }));

    probesCatalogServiceSpy.getGroupedById.and.returnValue(of({
      id: probeId,
      title: 'Перша проба',
      pointsCount: 2,
      sectionsCount: 1,
      sections: [
        {
          id: 'sec-1',
          code: 'A',
          title: 'Розділ A',
          points: [
            { id: 'point-1', title: 'Точка 1' },
            { id: 'point-2', title: 'Точка 2' }
          ]
        }
      ]
    }));

    memberProgressServiceSpy.getProbeProgress.and.returnValue(of({
      probeProgressKey: null,
      memberKey,
      kurinKey: 'kurin-1',
      probeId,
      status: ProbeProgressStatus.Completed,
      completedAtUtc: '2026-04-16T10:00:00Z',
      completedByUserKey: null,
      completedByName: 'Впорядник',
      completedByRole: 'Mentor',
      verifiedAtUtc: null,
      verifiedByUserKey: null,
      verifiedByName: null,
      verifiedByRole: null,
      auditTrail: []
    }));

    await TestBed.configureTestingModule({
      imports: [MemberProbePageComponent],
      providers: [
        { provide: MemberService, useValue: memberServiceSpy },
        { provide: ProbesCatalogService, useValue: probesCatalogServiceSpy },
        { provide: MemberProgressService, useValue: memberProgressServiceSpy },
        { provide: Router, useValue: routerSpy },
        { provide: ActivatedRoute, useValue: { paramMap: paramMapSubject.asObservable() } }
      ]
    }).compileComponents();
  });

  function createComponent() {
    fixture = TestBed.createComponent(MemberProbePageComponent);
    component = fixture.componentInstance;
  }

  it('should create and load probe sections', () => {
    createComponent();
    fixture.detectChanges();

    expect(component).toBeTruthy();
    expect(component.probeSections.length).toBe(1);
    expect(component.probeSections[0].points.length).toBe(2);
    expect(component.probeSections[0].points[0].isSigned).toBeTrue();
  });

  it('goBackToMember should navigate to member card route', () => {
    createComponent();
    fixture.detectChanges();

    component.goBackToMember();

    expect(routerSpy.navigate).toHaveBeenCalledWith(['/member', memberKey]);
  });
});
