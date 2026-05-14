import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { environment } from '../../../../environments/environment';
import { AccountSettings, AccountSettingsService } from './account-settings.service';

describe('AccountSettingsService', () => {
  let service: AccountSettingsService;
  let httpMock: HttpTestingController;
  const apiUrl = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AccountSettingsService]
    });

    service = TestBed.inject(AccountSettingsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should get account settings', (done) => {
    const settings = createSettings();

    service.getSettings().subscribe(response => {
      expect(response).toEqual(settings);
      done();
    });

    const req = httpMock.expectOne(`${apiUrl}/user/me`);
    expect(req.request.method).toBe('GET');
    expect(req.request.withCredentials).toBeTrue();
    req.flush(settings);
  });

  it('should update account profile', (done) => {
    const request = {
      email: 'new@example.com',
      phoneNumber: '123456789',
      currentPassword: 'current-password'
    };
    const settings = createSettings({ pendingEmail: 'new@example.com' });

    service.updateProfile(request).subscribe(response => {
      expect(response).toEqual(settings);
      done();
    });

    const req = httpMock.expectOne(`${apiUrl}/user/me`);
    expect(req.request.method).toBe('PUT');
    expect(req.request.body).toEqual(request);
    expect(req.request.withCredentials).toBeTrue();
    req.flush(settings);
  });

  it('should confirm email change', (done) => {
    const request = { email: 'confirmed@example.com', token: 'token' };
    const settings = createSettings({ email: request.email });

    service.confirmEmailChange(request).subscribe(response => {
      expect(response).toEqual(settings);
      done();
    });

    const req = httpMock.expectOne(`${apiUrl}/user/me/email/confirm`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    expect(req.request.withCredentials).toBeTrue();
    req.flush(settings);
  });

  it('should change password', (done) => {
    const request = {
      currentPassword: 'current-password',
      newPassword: 'new-password'
    };

    service.changePassword(request).subscribe(response => {
      expect(response).toBeTrue();
      done();
    });

    const req = httpMock.expectOne(`${apiUrl}/user/me/password`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    expect(req.request.withCredentials).toBeTrue();
    req.flush(true);
  });

  it('should reset mfa', (done) => {
    const request = { currentPassword: 'current-password' };

    service.resetMfa(request).subscribe(response => {
      expect(response).toBeTrue();
      done();
    });

    const req = httpMock.expectOne(`${apiUrl}/user/me/mfa/reset`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    expect(req.request.withCredentials).toBeTrue();
    req.flush(true);
  });

  it('should disable mfa', (done) => {
    const request = { currentPassword: 'current-password' };

    service.disableMfa(request).subscribe(response => {
      expect(response).toBeTrue();
      done();
    });

    const req = httpMock.expectOne(`${apiUrl}/user/me/mfa/disable`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    expect(req.request.withCredentials).toBeTrue();
    req.flush(true);
  });

  function createSettings(overrides: Partial<AccountSettings> = {}): AccountSettings {
    return {
      userKey: 'user-123',
      memberKey: 'member-123',
      email: 'user@example.com',
      phoneNumber: '123456789',
      firstName: 'John',
      lastName: 'Doe',
      role: 'Manager',
      twoFactorEnabled: false,
      pendingEmail: null,
      ...overrides
    };
  }
});
