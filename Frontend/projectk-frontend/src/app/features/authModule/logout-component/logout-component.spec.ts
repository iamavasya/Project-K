import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LogoutComponent } from './logout-component';
import { provideHttpClient } from '@angular/common/http';
import { AuthService } from '../services/authService/auth.service';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';

describe('LogoutComponent', () => {
  let component: LogoutComponent;
  let fixture: ComponentFixture<LogoutComponent>;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(async () => {
    mockAuthService = jasmine.createSpyObj('AuthService', ['logout']);
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [LogoutComponent],
      providers: [
        provideHttpClient(),
        { provide: AuthService, useValue: mockAuthService },
        { provide: Router, useValue: mockRouter }
      ],
    })
    .compileComponents();

    fixture = TestBed.createComponent(LogoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('logout', () => {
    it('should call authService.logout', () => {
      mockAuthService.logout.and.returnValue(of('Logged out successfully'));

      component.logout();

      expect(mockAuthService.logout).toHaveBeenCalled();
    });

    it('should navigate to /login on successful logout', () => {
      mockAuthService.logout.and.returnValue(of('Logged out successfully'));

      component.logout();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('should handle logout error', () => {
      const error = { status: 500, message: 'Server error' };
      mockAuthService.logout.and.returnValue(throwError(() => error));
      spyOn(console, 'log');

      component.logout();

      expect(console.log).toHaveBeenCalledWith(jasmine.stringContaining('Logout failed'));
    });

    it('should navigate to /login on logout error', () => {
      const error = { status: 500, message: 'Server error' };
      mockAuthService.logout.and.returnValue(throwError(() => error));
      spyOn(console, 'log');

      component.logout();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
    });
  });

  describe('Integration scenarios', () => {
    it('should complete full logout flow', () => {
      const message = 'Successfully logged out';
      mockAuthService.logout.and.returnValue(of(message));

      component.logout();

      expect(mockAuthService.logout).toHaveBeenCalled();
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('should handle network error gracefully and navigate', () => {
      const networkError = new Error('Network error');
      mockAuthService.logout.and.returnValue(throwError(() => networkError));
      spyOn(console, 'log');

      component.logout();

      expect(mockAuthService.logout).toHaveBeenCalled();
      expect(console.log).toHaveBeenCalledWith(jasmine.stringContaining('Logout failed'));
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('should handle 401 unauthorized error and navigate', () => {
      const error = { status: 401, message: 'Unauthorized' };
      mockAuthService.logout.and.returnValue(throwError(() => error));
      spyOn(console, 'log');

      component.logout();

      expect(console.log).toHaveBeenCalled();
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
    });
  });
});