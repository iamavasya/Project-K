import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LoginComponent } from './login-component';
import { provideHttpClient } from '@angular/common/http';
import { AuthService } from '../services/authService/auth.service';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthState } from '../models/auth-state.model';
import { LoginRequest } from '../models/login-request.model';

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    mockAuthService = jasmine.createSpyObj('AuthService', ['login', 'getAuthStateValue']);
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [LoginComponent],
      providers: [
        provideHttpClient(),
        { provide: AuthService, useValue: mockAuthService },
        { provide: Router, useValue: mockRouter }
      ],
    })
    .compileComponents();

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Component initialization', () => {
    it('should initialize with empty email and password', () => {
      expect(component.email).toBe('');
      expect(component.password).toBe('');
    });
  });

  describe('onSubmit', () => {
    it('should call authService.login with correct credentials', () => {
      const credentials: LoginRequest = {
        email: 'test@example.com',
        password: 'password123'
      };

      const mockAuthState: AuthState = {
        userKey: 'user-123',
        memberKey: 'test-member-key',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      component.email = credentials.email;
      component.password = credentials.password;

      mockAuthService.login.and.returnValue(of(mockAuthState));
      mockAuthService.getAuthStateValue.and.returnValue(mockAuthState);

      component.onSubmit();

      expect(mockAuthService.login).toHaveBeenCalledWith(credentials);
    });

    it('should navigate to /panel for Admin role', () => {
      const mockAuthState: AuthState = {
        userKey: 'user-123',
        memberKey: 'test-member-key',
        email: 'admin@example.com',
        role: 'Admin',
        kurinKey: null,
        accessToken: 'token-789'
      };

      component.email = 'admin@example.com';
      component.password = 'password123';

      mockAuthService.login.and.returnValue(of(mockAuthState));
      mockAuthService.getAuthStateValue.and.returnValue(mockAuthState);

      component.onSubmit();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/panel']);
    });

    it('should navigate to /member/:memberKey when memberKey is present and not Admin', () => {
      const mockAuthState: AuthState = {
        userKey: 'user-123',
        memberKey: 'test-member-key',
        email: 'manager@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      component.email = 'manager@example.com';
      component.password = 'password123';

      mockAuthService.login.and.returnValue(of(mockAuthState));
      mockAuthService.getAuthStateValue.and.returnValue(mockAuthState);

      component.onSubmit();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/member', 'test-member-key']);
    });

    it('should navigate to /kurin when no memberKey but has kurinKey', () => {
      const mockAuthState: AuthState = {
        userKey: 'user-123',
        memberKey: null,
        email: 'user@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      component.email = 'user@example.com';
      component.password = 'password123';

      mockAuthService.login.and.returnValue(of(mockAuthState));
      mockAuthService.getAuthStateValue.and.returnValue(mockAuthState);

      component.onSubmit();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/kurin']);
    });
    
    it('should navigate to / when no memberKey, no kurinKey and not Admin', () => {
      const mockAuthState: AuthState = {
        userKey: 'user-123',
        memberKey: null,
        email: 'user@example.com',
        role: 'Manager',
        kurinKey: null,
        accessToken: 'token-789'
      };

      component.email = 'user@example.com';
      component.password = 'password123';

      mockAuthService.login.and.returnValue(of(mockAuthState));
      mockAuthService.getAuthStateValue.and.returnValue(mockAuthState);

      component.onSubmit();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/']);
    });

    it('should handle login error', () => {
      const error = { status: 401, message: 'Unauthorized' };

      component.email = 'test@example.com';
      component.password = 'wrong-password';

      mockAuthService.login.and.returnValue(throwError(() => error));

      spyOn(window, 'alert');
      component.onSubmit();

      expect(window.alert).toHaveBeenCalledWith(jasmine.stringContaining('Login failed'));
    });

    it('should not navigate on login error', () => {
      const error = { status: 401, message: 'Unauthorized' };

      component.email = 'test@example.com';
      component.password = 'wrong-password';

      mockAuthService.login.and.returnValue(throwError(() => error));

      spyOn(window, 'alert');
      component.onSubmit();

      expect(mockRouter.navigate).not.toHaveBeenCalled();
    });

    it('should handle empty email and password', () => {
      const mockAuthState: AuthState = {
        userKey: 'user-123',
        memberKey: 'test-member-key',
        email: '',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      component.email = '';
      component.password = '';

      mockAuthService.login.and.returnValue(of(mockAuthState));
      mockAuthService.getAuthStateValue.and.returnValue(mockAuthState);

      component.onSubmit();

      expect(mockAuthService.login).toHaveBeenCalledWith({
        email: '',
        password: ''
      });
    });
  });

  describe('Form bindings', () => {
    it('should update email property when input changes', () => {
      component.email = 'newemail@example.com';
      expect(component.email).toBe('newemail@example.com');
    });

    it('should update password property when input changes', () => {
      component.password = 'newpassword';
      expect(component.password).toBe('newpassword');
    });
  });

  describe('Navigation logic', () => {
    it('should prioritize Admin navigation over memberKey or kurinKey', () => {
      const mockAuthState: AuthState = {
        userKey: 'user-123',
        memberKey: 'test-member-key',
        email: 'admin@example.com',
        role: 'Admin',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      component.email = 'admin@example.com';
      component.password = 'password123';

      mockAuthService.login.and.returnValue(of(mockAuthState));
      mockAuthService.getAuthStateValue.and.returnValue(mockAuthState);

      component.onSubmit();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/panel']);
      expect(mockRouter.navigate).not.toHaveBeenCalledWith(['/kurin']);
      expect(mockRouter.navigate).not.toHaveBeenCalledWith(['/member', 'test-member-key']);
    });

    it('should navigate to / when state is null', () => {
      const mockAuthState: AuthState = {
        userKey: 'user-123',
        memberKey: 'test-member-key',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: null,
        accessToken: 'token-789'
      };

      component.email = 'test@example.com';
      component.password = 'password123';

      mockAuthService.login.and.returnValue(of(mockAuthState));
      mockAuthService.getAuthStateValue.and.returnValue(null);

      component.onSubmit();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/']);
    });
  });

  describe('Integration scenarios', () => {
    it('should handle network error gracefully', () => {
      const networkError = new Error('Network error');

      component.email = 'test@example.com';
      component.password = 'password123';

      mockAuthService.login.and.returnValue(throwError(() => networkError));

      spyOn(window, 'alert');
      component.onSubmit();

      expect(window.alert).toHaveBeenCalledWith(jasmine.stringContaining('Login failed'));
      expect(mockRouter.navigate).not.toHaveBeenCalled();
      expect(mockAuthService.getAuthStateValue).not.toHaveBeenCalled();
    });
  });
});
