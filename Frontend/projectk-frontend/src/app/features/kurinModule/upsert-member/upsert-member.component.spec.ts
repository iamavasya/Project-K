import { ComponentFixture, TestBed } from '@angular/core/testing';
import { UpsertMemberComponent } from './upsert-member.component';
import { ActivatedRoute, Router, convertToParamMap, ParamMap, Navigation } from '@angular/router';
import { BehaviorSubject, of, throwError } from 'rxjs';
import { MemberService } from '../common/services/member-service/member.service';
import { ConfirmationService } from 'primeng/api';
import { MemberDto } from '../common/models/memberDto';
import { UpsertMemberDto } from '../common/models/requests/member/upsertMemberDto';
import { FileSelectEvent } from 'primeng/fileupload';
import { ImageCroppedEvent } from 'ngx-image-cropper';
import { HttpClient } from '@angular/common/http';
import { FormGroup } from '@angular/forms';
import { Location } from '@angular/common';

describe('UpsertMemberComponent', () => {
  let fixture: ComponentFixture<UpsertMemberComponent>;
  let component: UpsertMemberComponent;

  let routeParamMap$: BehaviorSubject<ParamMap>;
  let routerSpy: jasmine.SpyObj<Router>;
  let memberServiceSpy: jasmine.SpyObj<MemberService>;
  let confirmationServiceSpy: jasmine.SpyObj<ConfirmationService>;
  let locationSpy: jasmine.SpyObj<Location>;

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
    profilePhotoUrl: 'http://example.com/photo.jpg'
  };

  function setRouteParams(params: Record<string, string>) {
    routeParamMap$.next(convertToParamMap(params));
  }

  beforeEach(async () => {
    routeParamMap$ = new BehaviorSubject(convertToParamMap({ groupKey, memberKey }));
    routerSpy = jasmine.createSpyObj<Router>('Router', ['navigate', 'getCurrentNavigation']);
    routerSpy.getCurrentNavigation.and.returnValue(null);
    memberServiceSpy = jasmine.createSpyObj<MemberService>('MemberService', ['getByKey', 'create', 'update', 'delete']);
    confirmationServiceSpy = jasmine.createSpyObj<ConfirmationService>('ConfirmationService', ['confirm']);
    locationSpy = jasmine.createSpyObj<Location>('Location', ['back']);

    memberServiceSpy.getByKey.and.returnValue(of(loadedMember));
    memberServiceSpy.create.and.returnValue(of({ ...loadedMember, memberKey: 'created999' }));
    memberServiceSpy.update.and.returnValue(of(loadedMember));
    memberServiceSpy.delete.and.returnValue(of(void 0));

    confirmationServiceSpy.confirm.and.returnValue(confirmationServiceSpy);


    await TestBed.configureTestingModule({
      imports: [UpsertMemberComponent],
      providers: [
        { provide: ActivatedRoute, useValue: { paramMap: routeParamMap$.asObservable() } },
        { provide: Router, useValue: routerSpy },
        { provide: MemberService, useValue: memberServiceSpy },
        { provide: ConfirmationService, useValue: confirmationServiceSpy },
        { provide: Location, useValue: locationSpy },
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

  describe('Component initialization', () => {
    it('should initialize with default member object', () => {
      create();
      expect(component.member).toBeDefined();
      expect(component.member.memberKey).toBe(memberKey);
      expect(component.member.groupKey).toBe(groupKey);
    });

    it('should initialize with default values', () => {
      setRouteParams({ groupKey });
      create();
      expect(component.displayCropper).toBeFalse();
      expect(component.croppedImage).toBe('');
      expect(component.fileToUpload).toBeNull();
      expect(component.removeProfilePhoto).toBeFalse();
      expect(component.imageFile).toBeUndefined();
      expect(component.croppedFile).toBeNull();
    });

    it('should have injected dependencies', () => {
      create();
      expect(component.route).toBeDefined();
      expect(component.router).toBeDefined();
      expect(component.location).toBeDefined();
      expect(component.memberService).toBeDefined();
      expect(component.confirmationService).toBeDefined();
    });
  });

  describe('ngOnInit', () => {
    it('should load member in edit mode when memberKey present', () => {
      create();
      expect(component.isCreate).toBeFalse();
      expect(component.memberKey).toBe(memberKey);
      expect(component.groupKey).toBe(groupKey);
      expect(memberServiceSpy.getByKey).toHaveBeenCalledWith(memberKey);
      expect(component.member).toEqual(loadedMember);
    });

    it('should switch to create mode when memberKey absent', () => {
      setRouteParams({ groupKey });
      create();
      expect(component.isCreate).toBeTrue();
      expect(component.memberKey).toBeNull();
      expect(component.groupKey).toBe(groupKey);
      expect(memberServiceSpy.getByKey).not.toHaveBeenCalled();
    });

    it('should set groupKey and memberKey from route params', () => {
      create();
      expect(component.groupKey).toBe(groupKey);
      expect(component.memberKey).toBe(memberKey);
    });

    it('should navigate to group on load error', () => {
      memberServiceSpy.getByKey.and.returnValue(throwError(() => new Error('404')));
      spyOn(console, 'error');
      create();
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/group', groupKey], { replaceUrl: true });
      expect(console.error).toHaveBeenCalledWith('Error fetching member:', jasmine.any(Error));
    });

    it('should detect navigation from member page via getCurrentNavigation', () => {
      const mockNavigation: Partial<Navigation> = {
        extras: { state: { fromMember: true } }
      };
      routerSpy.getCurrentNavigation.and.returnValue(mockNavigation as Navigation);
      create();
      expect(component['cameFromMember']).toBeTrue();
    });

    it('should detect navigation from member page via history.state', () => {
      routerSpy.getCurrentNavigation.and.returnValue(null);
      spyOnProperty(history, 'state', 'get').and.returnValue({ fromMember: true });
      create();
      expect(component['cameFromMember']).toBeTrue();
    });

    it('should not set cameFromMember when state is missing', () => {
      routerSpy.getCurrentNavigation.and.returnValue(null);
      spyOnProperty(history, 'state', 'get').and.returnValue({});
      create();
      expect(component['cameFromMember']).toBeFalse();
    });
  });

  describe('submit - create mode', () => {
    beforeEach(() => {
      setRouteParams({ groupKey });
    });

    it('should create member with correct dto', () => {
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
      expect(dtoArg).toEqual({
        groupKey,
        firstName: 'Alice',
        middleName: 'B',
        lastName: 'Wonder',
        email: 'alice@example.com',
        phoneNumber: '555',
        dateOfBirth: '2012-07-09'
      });
      expect(fileArg).toBe(component.fileToUpload);
    });

    it('should navigate to created member on success', () => {
      create();
      component.member.firstName = 'Test';
      component.member.lastName = 'User';
      
      component.submit();

      expect(routerSpy.navigate).toHaveBeenCalledWith(['/member', 'created999'], { replaceUrl: true });
    });

    it('should create without file upload', () => {
      create();
      component.member.firstName = 'Test';
      component.member.lastName = 'User';
      component.fileToUpload = null;

      component.submit();

      const [, fileArg] = memberServiceSpy.create.calls.mostRecent().args;
      expect(fileArg).toBeNull();
    });

    it('should convert Date to yyyy-MM-dd format', () => {
      create();
      component.member.dateOfBirth = new Date('2011-01-02T14:00:00Z');
      component.submit();
      const dto = memberServiceSpy.create.calls.mostRecent().args[0] as UpsertMemberDto;
      expect(dto.dateOfBirth).toBe('2011-01-02');
    });

    it('should handle null dateOfBirth', () => {
      create();
      component.member.dateOfBirth = null;
      component.submit();
      const dto = memberServiceSpy.create.calls.mostRecent().args[0] as UpsertMemberDto;
      expect(dto.dateOfBirth).toBe('');
    });

    it('should handle string dateOfBirth in yyyy-MM-dd format', () => {
      create();
      component.member.dateOfBirth = new Date('2020-05-15');
      component.submit();
      const dto = memberServiceSpy.create.calls.mostRecent().args[0] as UpsertMemberDto;
      expect(dto.dateOfBirth).toBe('2020-05-15');
    });

    it('should log and handle create error', () => {
      const error = new Error('Create failed');
      memberServiceSpy.create.and.returnValue(throwError(() => error));
      spyOn(console, 'error');
      create();

      component.submit();

      expect(console.error).toHaveBeenCalledWith('Error creating member:', error, jasmine.any(Object));
      expect(routerSpy.navigate).not.toHaveBeenCalled();
    });
  });

  describe('submit - update mode', () => {
    it('should update member with correct dto', () => {
      create();
      component.member.firstName = 'Updated';
      component.removeProfilePhoto = true;

      component.submit();

      const args = memberServiceSpy.update.calls.mostRecent().args as [string, UpsertMemberDto, Blob | null];
      expect(args[0]).toBe(memberKey);
      const dtoArg = args[1];
      expect(dtoArg.firstName).toBe('Updated');
      expect(dtoArg.removeProfilePhoto).toBeTrue();
    });

    it('should convert Date to yyyy-MM-dd format', () => {
      create();
      component.member.dateOfBirth = new Date('1995-12-24T11:22:33Z');
      component.submit();
      const dto = memberServiceSpy.update.calls.mostRecent().args[1] as UpsertMemberDto;
      expect(dto.dateOfBirth).toBe('1995-12-24');
    });

    it('should navigate back when came from member page', () => {
      const mockNavigation: Partial<Navigation> = {
        extras: { state: { fromMember: true } }
      };
      routerSpy.getCurrentNavigation.and.returnValue(mockNavigation as Navigation);
      create();

      component.submit();

      expect(locationSpy.back).toHaveBeenCalled();
      expect(routerSpy.navigate).not.toHaveBeenCalled();
    });

    it('should navigate to member page when not came from member', () => {
      create();
      component.submit();

      expect(locationSpy.back).not.toHaveBeenCalled();
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/member', memberKey], { replaceUrl: true });
    });

    it('should include file in update request', () => {
      create();
      const file = new Blob(['data'], { type: 'image/png' });
      component.fileToUpload = file;

      component.submit();

      const args = memberServiceSpy.update.calls.mostRecent().args as [string, UpsertMemberDto, Blob | null];
      expect(args[2]).toBe(file);
    });

    it('should log and handle update error', () => {
      const error = new Error('Update failed');
      memberServiceSpy.update.and.returnValue(throwError(() => error));
      spyOn(console, 'error');
      create();

      component.submit();

      expect(console.error).toHaveBeenCalledWith('Error updating member:', error);
      expect(routerSpy.navigate).not.toHaveBeenCalled();
    });
  });

  describe('fileChangeEvent', () => {
    beforeEach(() => {
      setRouteParams({ groupKey });
      create();
    });

    it('should set file and open cropper', () => {
      const file = new File(['aa'], 'avatar.jpg', { type: 'image/jpeg' });
      const event: FileSelectEvent = { originalEvent: new Event('change'), files: [file], currentFiles: [file] };
      
      component.fileChangeEvent(event);
      
      expect(component.imageFile).toBe(file);
      expect(component.displayCropper).toBeTrue();
      expect(component.croppedImage).toBe('');
      expect(component.croppedFile).toBeNull();
      expect(component.removeProfilePhoto).toBeFalse();
    });

    it('should warn when no file selected', () => {
      spyOn(console, 'warn');
      const event: FileSelectEvent = { originalEvent: new Event('change'), files: [], currentFiles: [] };
      
      component.fileChangeEvent(event);
      
      expect(console.warn).toHaveBeenCalledWith('No file selected');
      expect(component.displayCropper).toBeFalse();
    });

    it('should revoke previous object URL', () => {
      spyOn(URL, 'revokeObjectURL');
      component['objectUrlToRevoke'] = 'blob://old-url';
      
      const file = new File(['aa'], 'avatar.jpg', { type: 'image/jpeg' });
      const event: FileSelectEvent = { originalEvent: new Event('change'), files: [file], currentFiles: [file] };
      
      component.fileChangeEvent(event);
      
      expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob://old-url');
      expect(component['objectUrlToRevoke']).toBeNull();
    });

    it('should not revoke URL when objectUrlToRevoke is null', () => {
      spyOn(URL, 'revokeObjectURL');
      component['objectUrlToRevoke'] = null;
      
      const file = new File(['aa'], 'avatar.jpg', { type: 'image/jpeg' });
      const event: FileSelectEvent = { originalEvent: new Event('change'), files: [file], currentFiles: [file] };
      
      component.fileChangeEvent(event);
      
      expect(URL.revokeObjectURL).not.toHaveBeenCalled();
    });
  });

  describe('imageCropped', () => {
    beforeEach(() => {
      setRouteParams({ groupKey });
      create();
    });

    it('should store base64 when base64 is provided', () => {
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

    it('should create File from blob when blob is provided', () => {
      const blob = new Blob(['xx'], { type: 'image/png' });
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
      expect(component.croppedFile!.name).toBe('orig.png');
      expect(component.croppedFile!.type).toBe('image/png');
    });

    it('should use default filename when imageFile is undefined', () => {
      const blob = new Blob(['xx'], { type: 'image/png' });
      component.imageFile = undefined;
      
      const cropEvent: ImageCroppedEvent = {
        width: 100,
        height: 100,
        imagePosition: { x1: 0, y1: 0, x2: 0, y2: 0 },
        cropperPosition: { x1: 0, y1: 0, x2: 0, y2: 0 },
        blob
      };
      
      component.imageCropped(cropEvent);
      
      expect(component.croppedFile!.name).toBe('profile.png');
    });

    it('should use objectUrl from event if available', () => {
      spyOn(URL, 'revokeObjectURL');
      const blob = new Blob(['xx'], { type: 'image/png' });
      component.imageFile = new File(['orig'], 'orig.jpeg', { type: 'image/jpeg' });
      component['objectUrlToRevoke'] = 'blob://old-url';
      
      const cropEvent: ImageCroppedEvent = {
        width: 100,
        height: 100,
        imagePosition: { x1: 0, y1: 0, x2: 0, y2: 0 },
        cropperPosition: { x1: 0, y1: 0, x2: 0, y2: 0 },
        blob,
        objectUrl: 'blob://new-url'
      };
      
      component.imageCropped(cropEvent);
      
      expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob://old-url');
      expect(component.croppedImage).toBe('blob://new-url');
      expect(component['objectUrlToRevoke']).toBe('blob://new-url');
    });

    it('should create objectUrl when not provided', () => {
      spyOn(URL, 'createObjectURL').and.returnValue('blob://created-url');
      const blob = new Blob(['xx'], { type: 'image/png' });
      component.imageFile = new File(['orig'], 'orig.jpeg', { type: 'image/jpeg' });
      
      const cropEvent: ImageCroppedEvent = {
        width: 100,
        height: 100,
        imagePosition: { x1: 0, y1: 0, x2: 0, y2: 0 },
        cropperPosition: { x1: 0, y1: 0, x2: 0, y2: 0 },
        blob
      };
      
      component.imageCropped(cropEvent);
      
      expect(URL.createObjectURL).toHaveBeenCalledWith(blob);
      expect(component.croppedImage).toBe('blob://created-url');
      expect(component['objectUrlToRevoke']).toBe('blob://created-url');
    });

    it('should not revoke same URL', () => {
      spyOn(URL, 'revokeObjectURL');
      const blob = new Blob(['xx'], { type: 'image/png' });
      component.imageFile = new File(['orig'], 'orig.jpeg', { type: 'image/jpeg' });
      component['objectUrlToRevoke'] = 'blob://same-url';
      
      const cropEvent: ImageCroppedEvent = {
        width: 100,
        height: 100,
        imagePosition: { x1: 0, y1: 0, x2: 0, y2: 0 },
        cropperPosition: { x1: 0, y1: 0, x2: 0, y2: 0 },
        blob,
        objectUrl: 'blob://same-url'
      };
      
      component.imageCropped(cropEvent);
      
      expect(URL.revokeObjectURL).not.toHaveBeenCalled();
    });

    it('should warn when neither base64 nor blob provided', () => {
      spyOn(console, 'warn');
      const cropEvent: ImageCroppedEvent = {
        width: 100,
        height: 100,
        imagePosition: { x1: 0, y1: 0, x2: 0, y2: 0 },
        cropperPosition: { x1: 0, y1: 0, x2: 0, y2: 0 }
      };
      
      component.imageCropped(cropEvent);
      
      expect(console.warn).toHaveBeenCalledWith('Crop event без base64 і blob', cropEvent);
    });
  });

  describe('save', () => {
    let mockForm: FormGroup;

    beforeEach(() => {
      setRouteParams({ groupKey });
      create();
      mockForm = jasmine.createSpyObj<FormGroup>('FormGroup', ['markAsTouched']);
    });

    it('should close cropper and use croppedFile', () => {
      const blob = new Blob(['out'], { type: 'image/png' });
      component.croppedFile = new File([blob], 'out.png', { type: 'image/png' });
      component.displayCropper = true;
      
      component.save(mockForm);
      
      expect(component.displayCropper).toBeFalse();
      expect(component.fileToUpload).toBe(component.croppedFile);
      expect(component.removeProfilePhoto).toBeFalse();
      expect(mockForm.markAsTouched).toHaveBeenCalled();
    });

    it('should convert base64 to blob when no croppedFile', () => {
      component.croppedImage = 'data:image/png;base64,iVBORw0KGgo=';
      component.croppedFile = null;
      
      component.save(mockForm);
      
      expect(component.fileToUpload).toBeTruthy();
      expect(component.fileToUpload).toBeInstanceOf(Blob);
      expect(component.removeProfilePhoto).toBeFalse();
      expect(mockForm.markAsTouched).toHaveBeenCalled();
    });

    it('should warn when no cropped image available', () => {
      spyOn(console, 'warn');
      component.croppedFile = null;
      component.croppedImage = '';
      
      component.save(mockForm);
      
      expect(console.warn).toHaveBeenCalledWith('Nothing to upload (cropped image undefined)');
      expect(component.fileToUpload).toBeNull();
    });
  });

  describe('onCancelCrop', () => {
    beforeEach(() => {
      setRouteParams({ groupKey });
      create();
    });

    it('should close cropper and clear croppedImage', () => {
      component.displayCropper = true;
      component.croppedImage = 'blob://some-url';
      
      component.onCancelCrop();
      
      expect(component.displayCropper).toBeFalse();
      expect(component.croppedImage).toBe('');
    });
  });

  describe('clearProfilePhoto', () => {
    let mockForm: FormGroup;

    beforeEach(() => {
      setRouteParams({ groupKey });
      create();
      mockForm = jasmine.createSpyObj<FormGroup>('FormGroup', ['markAsTouched']);
    });

    it('should clear all photo-related data', () => {
      spyOn(console, 'log');
      component.imageFile = new File(['test'], 'test.jpg');
      component.croppedImage = 'data:image/png;base64,AAA';
      component.croppedFile = new File(['cropped'], 'cropped.png');
      component.fileToUpload = new Blob(['upload']);
      component.member.profilePhotoUrl = 'http://example.com/photo.jpg';

      component.clearProfilePhoto(mockForm);

      expect(console.log).toHaveBeenCalledWith('Clearing profile photo');
      expect(component.imageFile).toBeUndefined();
      expect(component.croppedImage).toBe('');
      expect(component.croppedFile).toBeNull();
      expect(component.fileToUpload).toBeNull();
      expect(component.member.profilePhotoUrl).toBeNull();
      expect(component.removeProfilePhoto).toBeTrue();
      expect(mockForm.markAsTouched).toHaveBeenCalled();
    });

    it('should revoke object URL if exists', () => {
      spyOn(URL, 'revokeObjectURL');
      component['objectUrlToRevoke'] = 'blob://test-url';

      component.clearProfilePhoto(mockForm);

      expect(URL.revokeObjectURL).toHaveBeenCalledWith('blob://test-url');
      expect(component['objectUrlToRevoke']).toBeNull();
    });

    it('should not revoke when no object URL exists', () => {
      spyOn(URL, 'revokeObjectURL');
      component['objectUrlToRevoke'] = null;

      component.clearProfilePhoto(mockForm);

      expect(URL.revokeObjectURL).not.toHaveBeenCalled();
      expect(component.removeProfilePhoto).toBeTrue();
    });
  });

  describe('Integration scenarios', () => {
    it('should complete full create workflow with photo', () => {
      setRouteParams({ groupKey });
      create();

      // Upload file
      const file = new File(['photo'], 'photo.jpg', { type: 'image/jpeg' });
      const selectEvent: FileSelectEvent = { 
        originalEvent: new Event('change'), 
        files: [file], 
        currentFiles: [file] 
      };
      component.fileChangeEvent(selectEvent);
      expect(component.displayCropper).toBeTrue();

      // Crop image
      const blob = new Blob(['cropped'], { type: 'image/png' });
      const cropEvent: ImageCroppedEvent = {
        width: 100,
        height: 100,
        imagePosition: { x1: 0, y1: 0, x2: 0, y2: 0 },
        cropperPosition: { x1: 0, y1: 0, x2: 0, y2: 0 },
        blob
      };
      component.imageCropped(cropEvent);

      // Save cropped image
      const mockForm = jasmine.createSpyObj<FormGroup>('FormGroup', ['markAsTouched']);
      component.save(mockForm);
      expect(component.displayCropper).toBeFalse();
      expect(component.fileToUpload).toBeTruthy();

      // Submit form
      component.member.firstName = 'New';
      component.member.lastName = 'Member';
      component.member.email = 'new@example.com';
      component.submit();

      expect(memberServiceSpy.create).toHaveBeenCalled();
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/member', 'created999'], { replaceUrl: true });
    });

    it('should complete full update workflow with photo removal', () => {
      create();
      
      const mockForm = jasmine.createSpyObj<FormGroup>('FormGroup', ['markAsTouched']);
      
      // Clear photo
      component.clearProfilePhoto(mockForm);
      expect(component.removeProfilePhoto).toBeTrue();
      
      // Update member
      component.member.firstName = 'Updated';
      component.submit();

      const args = memberServiceSpy.update.calls.mostRecent().args as [string, UpsertMemberDto, Blob | null];
      const dto = args[1];
      expect(dto.removeProfilePhoto).toBeTrue();
      expect(routerSpy.navigate).toHaveBeenCalledWith(['/member', memberKey], { replaceUrl: true });
    });

    it('should handle cancel during photo crop', () => {
      setRouteParams({ groupKey });
      create();

      const file = new File(['photo'], 'photo.jpg', { type: 'image/jpeg' });
      const selectEvent: FileSelectEvent = { 
        originalEvent: new Event('change'), 
        files: [file], 
        currentFiles: [file] 
      };
      component.fileChangeEvent(selectEvent);
      expect(component.displayCropper).toBeTrue();

      component.onCancelCrop();

      expect(component.displayCropper).toBeFalse();
      expect(component.croppedImage).toBe('');
    });
  });
});