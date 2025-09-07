import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UpsertMemberComponent } from './upsert-member.component';
import { ActivatedRoute, Router, convertToParamMap, ParamMap } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { MemberService } from '../common/services/member-service/member.service';
import { ConfirmationService } from 'primeng/api';
import { MemberDto } from '../common/models/memberDto';
import { UpsertMemberDto } from '../common/models/requests/member/upsertMemberDto';
import { FileSelectEvent } from 'primeng/fileupload';
import { ImageCroppedEvent } from 'ngx-image-cropper';
import { HttpClient } from '@angular/common/http';
import { NgForm } from '@angular/forms';

describe('UpsertMemberComponent', () => {
  let fixture: ComponentFixture<UpsertMemberComponent>;
  let component: UpsertMemberComponent;

  let routeParamMap$: BehaviorSubject<ParamMap>;
  let routerSpy: jasmine.SpyObj<Router>;
  let memberServiceSpy: jasmine.SpyObj<MemberService>;
  let confirmationServiceSpy: jasmine.SpyObj<ConfirmationService>;

  const groupKey = 'group123';
  const memberKey = 'memberABC';

  const loadedMember: MemberDto = {
    memberKey,
    groupKey,
    kurinKey: 'kurin1',
    firstName: 'John',
    middleName: 'M',
    lastName: 'Doe',
    email: 'john@example.com',
    phoneNumber: '123456789',
    dateOfBirth: new Date('2000-05-10'),
    profilePhotoUrl: null
  };

  function setRouteParams(params: Record<string, string>) {
    routeParamMap$.next(convertToParamMap(params));
  }

  beforeEach(async () => {
    routeParamMap$ = new BehaviorSubject(convertToParamMap({ groupKey, memberKey }));
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate']);
    memberServiceSpy = jasmine.createSpyObj<MemberService>('MemberService', ['getByKey', 'create', 'update', 'delete']);
    confirmationServiceSpy = jasmine.createSpyObj<ConfirmationService>('ConfirmationService', ['confirm']);

    memberServiceSpy.getByKey.and.returnValue(of(loadedMember));
    memberServiceSpy.create.and.returnValue(of({ ...loadedMember, memberKey: 'created999' }));
    memberServiceSpy.update.and.returnValue(of(loadedMember));
    memberServiceSpy.delete.and.returnValue(of(void 0));

    await TestBed.configureTestingModule({
      imports: [UpsertMemberComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { paramMap: routeParamMap$.asObservable() } },
        { provide: Router, useValue: routerSpy },
        { provide: MemberService, useValue: memberServiceSpy },
        { provide: ConfirmationService, useValue: confirmationServiceSpy },
        { provide: HttpClient, useValue: {} },
      ]
    }).compileComponents();
  });

  function create() {
    fixture = TestBed.createComponent(UpsertMemberComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  it('should create', () => {
    create();
    expect(component).toBeTruthy();
  });

  it('should load member in edit mode when memberKey present', () => {
    create();
    expect(component.isCreate).toBeFalse();
    expect(memberServiceSpy.getByKey).toHaveBeenCalledWith(memberKey);
    expect(component.member.memberKey).toBe(memberKey);
  });

  it('should switch to create mode when memberKey absent', () => {
    setRouteParams({ groupKey });
    create();
    expect(component.isCreate).toBeTrue();
    expect(component.memberKey).toBeNull();
    expect(memberServiceSpy.getByKey).not.toHaveBeenCalled();
  });

  it('should navigate to group on load error', () => {
    memberServiceSpy.getByKey.and.returnValue(throwError(() => new Error('404')));
    create();
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/group', groupKey], { replaceUrl: true });
  });

  it('submit (create) should send correct dto and navigate to created member', () => {
    setRouteParams({ groupKey });
    create();
    component.member.firstName = 'Alice';
    component.member.middleName = 'B';
    component.member.lastName = 'Wonder';
    component.member.email = 'alice@example.com';
    component.member.phoneNumber = '555';
    component.member.dateOfBirth = new Date('2012-07-09T15:33:00Z');
    component.fileToUpload = new Blob(['x'], { type: 'image/png' });

    component.submit();

    expect(memberServiceSpy.create).toHaveBeenCalledTimes(1);
    const [dtoArg, fileArg] = memberServiceSpy.create.calls.mostRecent().args as [UpsertMemberDto, Blob | null];
    expect(dtoArg).toEqual(jasmine.objectContaining({
      groupKey,
      firstName: 'Alice',
      middleName: 'B',
      lastName: 'Wonder',
      email: 'alice@example.com',
      phoneNumber: '555',
      dateOfBirth: '2012-07-09'
    }));
    expect(fileArg).toBe(component.fileToUpload);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/member', 'created999']);
  });

  it('submit (update) should convert Date to yyyy-MM-dd', () => {
    create();
    const dateObj = new Date('1995-12-24T11:22:33Z');
    component.member.dateOfBirth = dateObj;
    component.submit();
    const dto = memberServiceSpy.update.calls.mostRecent().args[1] as UpsertMemberDto;
    expect(dto.dateOfBirth).toBe('1995-12-24');
  });

  it('submit (create) should keep date-only string produced from Date', () => {
    setRouteParams({ groupKey });
    create();
    component.member.dateOfBirth = new Date('2011-01-02T14:00:00Z');
    component.submit();
    const dto = memberServiceSpy.create.calls.mostRecent().args[0] as UpsertMemberDto;
    expect(dto.dateOfBirth).toBe('2011-01-02');
  });

  describe('file & crop workflow', () => {
    let mockForm: NgForm;
    
    beforeEach(() => {
      setRouteParams({ groupKey });
      create();
      mockForm = jasmine.createSpyObj<NgForm>('NgForm', [], {
        form: jasmine.createSpyObj('FormGroup', ['markAsTouched'])
      });
    });

    it('fileChangeEvent should set file and open cropper', () => {
      const f = new File(['aa'], 'avatar.jpg', { type: 'image/jpeg' });
      const event: FileSelectEvent = { originalEvent: new Event('change'), files: [f], currentFiles: [f] };
      component.fileChangeEvent(event);
      expect(component.imageFile).toBe(f);
      expect(component.displayCropper).toBeTrue();
      expect(component.croppedImage).toBe('');
      expect(component.croppedFile).toBeNull();
    });

    it('fileChangeEvent with empty array should keep cropper closed', () => {
      const event: FileSelectEvent = { originalEvent: new Event('change'), files: [], currentFiles: [] };
      component.fileChangeEvent(event);
      expect(component.displayCropper).toBeFalse();
    });

    it('imageCropped base64 path stores base64 and null file', () => {
      const cropEvent: ImageCroppedEvent = {
        width: 100,
        height: 100,
        imagePosition: { x1: 0, y1: 0, x2: 0, y2: 0 },
        cropperPosition: { x1: 0, y1: 0, x2: 0, y2: 0 },
        base64: 'data:image/png;base64,AAA'
      };
      component.imageCropped(cropEvent);
      expect(component.croppedImage).toBe('data:image/png;base64,AAA');
      expect(component.croppedFile).toBeNull();
    });

    it('imageCropped blob path creates file & object URL', () => {
      const blob = new Blob(['xx'], { type: 'image/png' });
      spyOn(URL, 'createObjectURL').and.returnValue('blob://preview1');
      component.imageFile = new File(['orig'], 'orig.jpeg', { type: 'image/jpeg' });
      const cropEvent: ImageCroppedEvent = {
        width: 100,
        height: 100,
        imagePosition: { x1: 0, y1: 0, x2: 0, y2: 0 },
        cropperPosition: { x1: 0, y1: 0, x2: 0, y2: 0 },
        blob
      };
      component.imageCropped(cropEvent);
      expect(component.croppedFile).toBeTruthy();
      expect(component.croppedFile!.name.endsWith('.png')).toBeTrue();
      expect(component.croppedImage).toBe('blob://preview1');
    });

    it('save should take croppedFile when present', () => {
      const blob = new Blob(['out'], { type: 'image/png' });
      component.croppedFile = new File([blob], 'out.png', { type: 'image/png' });
      component.displayCropper = true;
      component.save(mockForm.form);
      expect(component.displayCropper).toBeFalse();
      expect(component.fileToUpload).toBe(component.croppedFile);
    });

    it('save should convert base64 when no croppedFile', () => {
      component.croppedImage = 'data:image/png;base64,iVBORw0KGgo=';
      component.croppedFile = null;
      component.save(mockForm.form);
      expect(component.fileToUpload).toBeTruthy();
    });

    it('onCancelCrop resets flags', () => {
      component.displayCropper = true;
      component.croppedImage = 'something';
      component.onCancelCrop();
      expect(component.displayCropper).toBeFalse();
      expect(component.croppedImage).toBe('');
    });
  });
});