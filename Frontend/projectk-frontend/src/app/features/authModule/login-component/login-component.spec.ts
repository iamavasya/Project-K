import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { ActivatedRoute, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { of, throwError } from 'rxjs';
import { LoginComponent } from './login-component';
import { AuthState } from '../models/auth-state.model';
import { LoginResponse } from '../models/login-response.model';
import { AuthService } from '../services/authService/auth.service';
import { PermissionService } from '../services/permission.service';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authService: jasmine.SpyObj<AuthService>;
  let permissionService: jasmine.SpyObj<PermissionService>;
  let router: jasmine.SpyObj<Router>;
  let messageService: jasmine.SpyObj<MessageService>;

  beforeEach(async () => {
    authService = jasmine.createSpyObj('AuthService', ['login', 'verifyMfaLogin', 'getAuthStateValue']);
    permissionService = jasmine.createSpyObj('PermissionService', ['isAdmin']);
    router = jasmine.createSpyObj('Router', ['navigate']);
    messageService = jasmine.createSpyObj('MessageService', ['add']);

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideHttpClient(),
        { provide: AuthService, useValue: authService },
        { provide: PermissionService, useValue: permissionService },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: {} },
        { provide: MessageService, useValue: messageService }
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should submit credentials to auth service', () => {
    const state = createAuthState();
    authService.login.and.returnValue(of(createLoginResponse()));
    authService.getAuthStateValue.and.returnValue(state);
    permissionService.isAdmin.and.returnValue(false);

    component.email = 'test@example.com';
    component.password = 'password123';

    component.onSubmit();

    expect(authService.login).toHaveBeenCalledWith({
      email: 'test@example.com',
      password: 'password123'
    });
  });

  it('should show otp input when login requires mfa', () => {
    authService.login.and.returnValue(of(createLoginResponse({ requiresMfa: true, tokens: null })));

    component.email = 'mfa@example.com';
    component.password = 'password123';
    component.otpValue = '123456';
    component.useRecoveryCode = true;

    component.onSubmit();

    expect(component.showOtpInput).toBeTrue();
    expect(component.otpValue).toBe('');
    expect(component.useRecoveryCode).toBeFalse();
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('should verify otp and navigate after mfa succeeds', () => {
    const state = createAuthState({ memberKey: 'member-123' });
    authService.verifyMfaLogin.and.returnValue(of(state));
    authService.getAuthStateValue.and.returnValue(state);
    permissionService.isAdmin.and.returnValue(false);

    component.email = 'mfa@example.com';
    component.showOtpInput = true;
    component.otpValue = '123456';

    component.onSubmit();

    expect(authService.verifyMfaLogin).toHaveBeenCalledWith('mfa@example.com', '123456');
    expect(router.navigate).toHaveBeenCalledWith(['/member', 'member-123']);
  });

  it('should navigate to admin panel for admin role', () => {
    const state = createAuthState({ role: 'Admin', memberKey: 'member-123', kurinKey: 'kurin-123' });
    authService.login.and.returnValue(of(createLoginResponse()));
    authService.getAuthStateValue.and.returnValue(state);
    permissionService.isAdmin.and.returnValue(true);

    component.email = 'admin@example.com';
    component.password = 'password123';

    component.onSubmit();

    expect(router.navigate).toHaveBeenCalledWith(['/panel']);
  });

  it('should navigate to member page when member key is present', () => {
    const state = createAuthState({ memberKey: 'member-123' });
    authService.login.and.returnValue(of(createLoginResponse()));
    authService.getAuthStateValue.and.returnValue(state);
    permissionService.isAdmin.and.returnValue(false);

    component.email = 'manager@example.com';
    component.password = 'password123';

    component.onSubmit();

    expect(router.navigate).toHaveBeenCalledWith(['/member', 'member-123']);
  });

  it('should navigate to kurin page when only kurin key is present', () => {
    const state = createAuthState({ memberKey: null, kurinKey: 'kurin-123' });
    authService.login.and.returnValue(of(createLoginResponse()));
    authService.getAuthStateValue.and.returnValue(state);
    permissionService.isAdmin.and.returnValue(false);

    component.email = 'manager@example.com';
    component.password = 'password123';

    component.onSubmit();

    expect(router.navigate).toHaveBeenCalledWith(['/kurin']);
  });

  it('should navigate to login when authenticated state has no destination keys', () => {
    const state = createAuthState({ memberKey: null, kurinKey: null });
    authService.login.and.returnValue(of(createLoginResponse()));
    authService.getAuthStateValue.and.returnValue(state);
    permissionService.isAdmin.and.returnValue(false);

    component.email = 'user@example.com';
    component.password = 'password123';

    component.onSubmit();

    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should show message when login fails', () => {
    authService.login.and.returnValue(throwError(() => ({ error: { message: 'Bad credentials' } })));

    component.email = 'user@example.com';
    component.password = 'wrong-password';

    component.onSubmit();

    expect(messageService.add).toHaveBeenCalledWith(jasmine.objectContaining({
      severity: 'error',
      detail: 'Bad credentials'
    }));
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('should sanitize otp input to six digits', () => {
    component.onOtpChange('12a-34567');

    expect(component.otpValue).toBe('123456');
  });

  it('should switch between password and recovery code modes', () => {
    component.otpValue = '123456';

    component.toggleRecoveryCode();

    expect(component.useRecoveryCode).toBeTrue();
    expect(component.otpValue).toBe('');
  });

  it('should return to password step from otp step', () => {
    component.showOtpInput = true;
    component.otpValue = '123456';
    component.useRecoveryCode = true;

    component.backToPassword();

    expect(component.showOtpInput).toBeFalse();
    expect(component.otpValue).toBe('');
    expect(component.useRecoveryCode).toBeFalse();
  });

  function createAuthState(overrides: Partial<AuthState> = {}): AuthState {
    return {
      userKey: 'user-123',
      memberKey: 'member-123',
      email: 'test@example.com',
      role: 'Manager',
      kurinKey: 'kurin-123',
      accessToken: 'token-123',
      ...overrides
    };
  }

  function createLoginResponse(overrides: Partial<LoginResponse> = {}): LoginResponse {
    return {
      userKey: 'user-123',
      memberKey: 'member-123',
      email: 'test@example.com',
      role: 'Manager',
      kurinKey: 'kurin-123',
      requiresMfa: false,
      tokens: { accessToken: 'token-123' },
      ...overrides
    };
  }
});
