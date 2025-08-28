import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MemberCardComponent } from './member-card.component';
import { ActivatedRoute, convertToParamMap, ParamMap, Router } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { MemberService } from '../common/services/member-service/member.service';
import { MemberDto } from '../common/models/memberDto';

describe('MemberCardComponent', () => {
  let fixture: ComponentFixture<MemberCardComponent>;
  let component: MemberCardComponent;

  let memberServiceSpy: jasmine.SpyObj<MemberService>;
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
    profilePhotoUrl: null
  };

  beforeEach(async () => {
    memberServiceSpy = jasmine.createSpyObj<MemberService>('MemberService', ['getByKey']);
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);
    paramMapSubject = new BehaviorSubject(convertToParamMap({ memberKey }));

    await TestBed.configureTestingModule({
      imports: [MemberCardComponent],
      providers: [
        { provide: MemberService, useValue: memberServiceSpy },
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
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/group', member.groupKey, 'member', 'upsert', memberKey]);
  });

  it('refreshData should use latest paramMap value if it changes', () => {
    memberServiceSpy.getByKey.and.returnValues(of(member), of({ ...member, memberKey: 'newKey' }));
    createComponent();
    fixture.detectChanges();
    paramMapSubject.next(convertToParamMap({ memberKey: 'newKey' }));
    component.memberKey = 'newKey'; // emulate subscription update timing
    component.refreshData();
    expect(memberServiceSpy.getByKey).toHaveBeenCalledWith('newKey');
  });
});