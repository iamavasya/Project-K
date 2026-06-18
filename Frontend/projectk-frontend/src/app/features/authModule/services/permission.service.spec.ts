import { TestBed } from '@angular/core/testing';
import { PermissionService } from './permission.service';
import { AuthService } from './authService/auth.service';

describe('PermissionService', () => {
  let service: PermissionService;
  let authService: jasmine.SpyObj<AuthService>;

  beforeEach(() => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['getAuthStateValue']);
    authService.getAuthStateValue.and.returnValue(null);

    TestBed.configureTestingModule({
      providers: [
        PermissionService,
        { provide: AuthService, useValue: authService }
      ]
    });

    service = TestBed.inject(PermissionService);
  });

  it('allows admins and managers to manage kurin settings', () => {
    expect(service.canManageKurinSettings('Admin')).toBeTrue();
    expect(service.canManageKurinSettings('Manager')).toBeTrue();
  });

  it('does not allow lower roles to manage kurin settings', () => {
    expect(service.canManageKurinSettings('Mentor')).toBeFalse();
    expect(service.canManageKurinSettings('User')).toBeFalse();
    expect(service.canManageKurinSettings(null)).toBeFalse();
  });
});
