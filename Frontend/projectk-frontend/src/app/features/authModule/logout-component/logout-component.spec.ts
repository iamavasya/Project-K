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
      spyOn(window, 'alert');

      component.logout();

      expect(mockAuthService.logout).toHaveBeenCalled();
    });

    it('should show alert on successful logout', () => {
      const message = 'Logged out successfully';
      mockAuthService.logout.and.returnValue(of(message));
      spyOn(window, 'alert');

      component.logout();

      expect(window.alert).toHaveBeenCalledWith(message);
    });

    it('should navigate to /login on successful logout', () => {
      mockAuthService.logout.and.returnValue(of('Logged out successfully'));
      spyOn(window, 'alert');

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

    it('should not navigate on logout error', () => {
      const error = { status: 500, message: 'Server error' };
      mockAuthService.logout.and.returnValue(throwError(() => error));
      spyOn(console, 'log');

      component.logout();

      expect(mockRouter.navigate).not.toHaveBeenCalled();
    });

    it('should not show alert on logout error', () => {
      const error = { status: 500, message: 'Server error' };
      mockAuthService.logout.and.returnValue(throwError(() => error));
      spyOn(window, 'alert');
      spyOn(console, 'log');

      component.logout();

      expect(window.alert).not.toHaveBeenCalled();
    });
  });

  describe('Integration scenarios', () => {
    it('should complete full logout flow', () => {
      const message = 'Successfully logged out';
      mockAuthService.logout.and.returnValue(of(message));
      spyOn(window, 'alert');

      component.logout();

      expect(mockAuthService.logout).toHaveBeenCalled();
      expect(window.alert).toHaveBeenCalledWith(message);
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/login']);
    });

    it('should handle network error gracefully', () => {
      const networkError = new Error('Network error');
      mockAuthService.logout.and.returnValue(throwError(() => networkError));
      spyOn(console, 'log');

      component.logout();

      expect(mockAuthService.logout).toHaveBeenCalled();
      expect(console.log).toHaveBeenCalledWith(jasmine.stringContaining('Logout failed'));
      expect(mockRouter.navigate).not.toHaveBeenCalled();
    });

    it('should handle 401 unauthorized error', () => {
      const error = { status: 401, message: 'Unauthorized' };
      mockAuthService.logout.and.returnValue(throwError(() => error));
      spyOn(console, 'log');

      component.logout();

      expect(console.log).toHaveBeenCalled();
      expect(mockRouter.navigate).not.toHaveBeenCalled();
    });
  });
});