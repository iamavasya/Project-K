import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, Router } from '@angular/router';
import { of } from 'rxjs';
import { MemberService } from '../common/services/member-service/member.service';
import { PlanningService } from '../common/services/planning-service/planning-service';

import { PlanningListComponent } from './planning-list';

describe('PlanningList', () => {
  let component: PlanningListComponent;
  let fixture: ComponentFixture<PlanningListComponent>;

  beforeEach(async () => {
    const planningServiceSpy = jasmine.createSpyObj('PlanningService', ['getSessions', 'deleteSession']);
    planningServiceSpy.getSessions.and.returnValue(of([]));
    planningServiceSpy.deleteSession.and.returnValue(of('ok'));

    const memberServiceSpy = jasmine.createSpyObj('MemberService', ['getAll']);
    memberServiceSpy.getAll.and.returnValue(of([]));

    const routerSpy = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [PlanningListComponent],
      providers: [
        { provide: PlanningService, useValue: planningServiceSpy },
        { provide: MemberService, useValue: memberServiceSpy },
        { provide: Router, useValue: routerSpy },
        {
          provide: ActivatedRoute,
          useValue: {
            paramMap: of(convertToParamMap({ kurinKey: 'k1' }))
          }
        }
      ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(PlanningListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
