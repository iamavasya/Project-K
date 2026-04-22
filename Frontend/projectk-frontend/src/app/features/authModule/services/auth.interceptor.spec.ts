import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HTTP_INTERCEPTORS, HttpClient } from '@angular/common/http';
import { AuthInterceptor } from './auth.interceptor';
import { AuthService } from './authService/auth.service';
import { Router } from '@angular/router';
import { throwError } from 'rxjs';

describe('AuthInterceptor', () => {
  let httpMock: HttpTestingController;
  let httpClient: HttpClient;
  let mockAuthService: jasmine.SpyObj<AuthService>;
  let mockRouter: jasmine.SpyObj<Router>;

  beforeEach(() => {
    mockAuthService = jasmine.createSpyObj('AuthService', ['getAccessToken', 'refreshToken', 'clearLocalState']);
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        { provide: AuthService, useValue: mockAuthService },
        { provide: Router, useValue: mockRouter },
        { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true }
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
    httpClient = TestBed.inject(HttpClient);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should not try to refresh token if 401 occurs on /api/auth/logout', () => {
    mockAuthService.getAccessToken.and.returnValue('valid-token');

    httpClient.post('/api/auth/logout', {}).subscribe({
      next: () => fail('should have failed with 401'),
      error: (error) => {
        expect(error.status).toBe(401);
      }
    });

    const req = httpMock.expectOne('/api/auth/logout');
    expect(req.request.headers.get('Authorization')).toBe('Bearer valid-token');
    
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });

    expect(mockAuthService.refreshToken).not.toHaveBeenCalled();
  });

  it('should call clearLocalState and navigate to login when refresh fails', () => {
    mockAuthService.getAccessToken.and.returnValue('valid-token');
    mockAuthService.refreshToken.and.returnValue(throwError(() => new Error('refresh failed')));

    httpClient.get('/api/data').subscribe({
      next: () => fail('should have failed'),
      error: (error) => {
        expect(error.message).toBe('Session expired');
        expect(mockAuthService.clearLocalState).toHaveBeenCalled();
        expect(mockRouter.navigate).toHaveBeenCalledWith(['/login'], { replaceUrl: true });
      }
    });

    const req = httpMock.expectOne('/api/data');
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
  });
});
