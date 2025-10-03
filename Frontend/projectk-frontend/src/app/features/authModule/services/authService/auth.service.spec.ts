import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { LoginRequest } from '../../models/login-request.model';
import { LoginResponse } from '../../models/login-response.model';
import { AuthState } from '../../models/auth-state.model';
import { KurinDto } from '../../../kurinModule/common/models/kurinDto';
import { environment } from '../../../environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  const apiUrl = environment.apiUrl;

  beforeEach(() => {
    localStorage.clear();
    
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuthService]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('constructor', () => {
    it('should load auth state from localStorage on initialization', () => {
      const mockAuthState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      localStorage.setItem('authState', JSON.stringify(mockAuthState));

      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        imports: [HttpClientTestingModule],
        providers: [AuthService]
      });

      const newService = TestBed.inject(AuthService);
      
      newService.getAuthState().subscribe(state => {
        expect(state).toEqual(mockAuthState);
      });
    });

    it('should have null auth state when localStorage is empty', () => {
      service.getAuthState().subscribe(state => {
        expect(state).toBeNull();
      });
    });
  });

  describe('getAuthState', () => {
    it('should return auth state as observable', (done) => {
      const mockAuthState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      service.login({ email: 'test@example.com', password: 'password' }).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/auth/login`);
      req.flush({
        userKey: mockAuthState.userKey,
        email: mockAuthState.email,
        role: mockAuthState.role,
        kurinKey: mockAuthState.kurinKey,
        tokens: { accessToken: mockAuthState.accessToken }
      });

      service.getAuthState().subscribe(state => {
        expect(state).toEqual(mockAuthState);
        done();
      });
    });
  });

  describe('getAuthStateValue', () => {
    it('should return current auth state value', () => {
      expect(service.getAuthStateValue()).toBeNull();
    });

    it('should return updated auth state value after login', (done) => {
      const mockAuthState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      service.login({ email: 'test@example.com', password: 'password' }).subscribe(() => {
        expect(service.getAuthStateValue()).toEqual(mockAuthState);
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/auth/login`);
      req.flush({
        userKey: mockAuthState.userKey,
        email: mockAuthState.email,
        role: mockAuthState.role,
        kurinKey: mockAuthState.kurinKey,
        tokens: { accessToken: mockAuthState.accessToken }
      });
    });
  });

  describe('login', () => {
    it('should send login request and update auth state', (done) => {
      const credentials: LoginRequest = {
        email: 'test@example.com',
        password: 'password123'
      };

      const mockResponse: LoginResponse = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        tokens: {
          accessToken: 'access-token-789'
        }
      };

      const expectedState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'access-token-789'
      };

      service.login(credentials).subscribe(state => {
        expect(state).toEqual(expectedState);
        expect(service.getAuthStateValue()).toEqual(expectedState);
        expect(localStorage.getItem('authState')).toBe(JSON.stringify(expectedState));
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/auth/login`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(credentials);
      expect(req.request.withCredentials).toBeTrue();
      req.flush(mockResponse);
    });

    it('should handle login error', (done) => {
      const credentials: LoginRequest = {
        email: 'test@example.com',
        password: 'wrong-password'
      };

      service.login(credentials).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          expect(error.status).toBe(401);
          expect(service.getAuthStateValue()).toBeNull();
          done();
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/auth/login`);
      req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    });
  });

  describe('logout', () => {
    it('should send logout request and clear auth state', (done) => {
      const mockAuthState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      service.login({ email: 'test@example.com', password: 'password' }).subscribe(() => {
        service.logout().subscribe(() => {
          expect(service.getAuthStateValue()).toBeNull();
          expect(localStorage.getItem('authState')).toBeNull();
          done();
        });

        const logoutReq = httpMock.expectOne(`${apiUrl}/auth/logout`);
        expect(logoutReq.request.method).toBe('POST');
        expect(logoutReq.request.withCredentials).toBeTrue();
        expect(logoutReq.request.responseType).toBe('text');
        logoutReq.flush('Logged out successfully');
      });

      const loginReq = httpMock.expectOne(`${apiUrl}/auth/login`);
      loginReq.flush({
        userKey: mockAuthState.userKey,
        email: mockAuthState.email,
        role: mockAuthState.role,
        kurinKey: mockAuthState.kurinKey,
        tokens: { accessToken: mockAuthState.accessToken }
      });
    });

    it('should not clear auth state if logout request fails', (done) => {
      const mockAuthState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      service.login({ email: 'test@example.com', password: 'password' }).subscribe(() => {
        service.logout().subscribe({
          next: () => fail('should have failed'),
          error: () => {
            expect(service.getAuthStateValue()).toEqual(mockAuthState);
            expect(localStorage.getItem('authState')).toBe(JSON.stringify(mockAuthState));
            done();
          }
        });

        const logoutReq = httpMock.expectOne(`${apiUrl}/auth/logout`);
        logoutReq.flush('Server error', { status: 500, statusText: 'Internal Server Error' });
      });

      const loginReq = httpMock.expectOne(`${apiUrl}/auth/login`);
      loginReq.flush({
        userKey: mockAuthState.userKey,
        email: mockAuthState.email,
        role: mockAuthState.role,
        kurinKey: mockAuthState.kurinKey,
        tokens: { accessToken: mockAuthState.accessToken }
      });
    });
  });

  describe('refreshToken', () => {
    it('should refresh access token and update auth state', (done) => {
      const initialState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'old-token'
      };

      const newAccessToken = 'new-access-token';

      service.login({ email: 'test@example.com', password: 'password' }).subscribe(() => {
        service.refreshToken().subscribe(token => {
          expect(token).toBe(newAccessToken);
          
          const updatedState = service.getAuthStateValue();
          expect(updatedState?.accessToken).toBe(newAccessToken);
          expect(updatedState?.userKey).toBe(initialState.userKey);
          expect(updatedState?.email).toBe(initialState.email);
          
          const savedState = localStorage.getItem('authState');
          expect(savedState).toBeTruthy();
          const parsedState = JSON.parse(savedState!);
          expect(parsedState.accessToken).toBe(newAccessToken);
          done();
        });

        const refreshReq = httpMock.expectOne(`${apiUrl}/auth/refresh`);
        expect(refreshReq.request.method).toBe('POST');
        expect(refreshReq.request.withCredentials).toBeTrue();
        refreshReq.flush({ accessToken: newAccessToken });
      });

      const loginReq = httpMock.expectOne(`${apiUrl}/auth/login`);
      loginReq.flush({
        userKey: initialState.userKey,
        email: initialState.email,
        role: initialState.role,
        kurinKey: initialState.kurinKey,
        tokens: { accessToken: initialState.accessToken }
      });
    });

    it('should not update state if no auth state exists', (done) => {
      service.refreshToken().subscribe(token => {
        expect(token).toBe('new-token');
        expect(service.getAuthStateValue()).toBeNull();
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/auth/refresh`);
      req.flush({ accessToken: 'new-token' });
    });

    it('should handle refresh token error', (done) => {
      service.refreshToken().subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          expect(error.status).toBe(401);
          done();
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/auth/refresh`);
      req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
    });
  });

  describe('registerFirstManager', () => {
    it('should send registration request with correct data', (done) => {
      const kurinDto: KurinDto = {
        kurinKey: 'kurin-key',
        number: 42,
        managerEmail: 'manager@test.com'
      };

      const expectedBody = {
        email: 'manager@test.com',
        password: null,
        firstName: null,
        lastName: null,
        phoneNumber: null,
        kurinNumber: 42,
        role: 'Manager'
      };

      service.registerFirstManager(kurinDto).subscribe(() => {
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/auth/register/manager`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(expectedBody);
      expect(req.request.withCredentials).toBeTrue();
      req.flush(null);
    });

    it('should handle registration error', (done) => {
      const kurinDto: KurinDto = {
        kurinKey: 'kurin-key',
        number: 42,
        managerEmail: 'manager@test.com'
      };

      service.registerFirstManager(kurinDto).subscribe({
        next: () => fail('should have failed'),
        error: (error) => {
          expect(error.status).toBe(400);
          done();
        }
      });

      const req = httpMock.expectOne(`${apiUrl}/auth/register/manager`);
      req.flush('Bad Request', { status: 400, statusText: 'Bad Request' });
    });
  });

  describe('getAccessToken', () => {
    it('should return null when no auth state exists', () => {
      expect(service.getAccessToken()).toBeNull();
    });

    it('should return access token when auth state exists', (done) => {
      const mockAuthState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      service.login({ email: 'test@example.com', password: 'password' }).subscribe(() => {
        expect(service.getAccessToken()).toBe('token-789');
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/auth/login`);
      req.flush({
        userKey: mockAuthState.userKey,
        email: mockAuthState.email,
        role: mockAuthState.role,
        kurinKey: mockAuthState.kurinKey,
        tokens: { accessToken: mockAuthState.accessToken }
      });
    });
  });

  describe('setKurinKey', () => {
    it('should update kurin key in auth state', (done) => {
      const mockAuthState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      service.login({ email: 'test@example.com', password: 'password' }).subscribe(() => {
        service.setKurinKey('new-kurin-key');

        const updatedState = service.getAuthStateValue();
        expect(updatedState?.kurinKey).toBe('new-kurin-key');
        expect(updatedState?.userKey).toBe(mockAuthState.userKey);
        
        const savedState = localStorage.getItem('authState');
        expect(savedState).toBeTruthy();
        const parsedState = JSON.parse(savedState!);
        expect(parsedState.kurinKey).toBe('new-kurin-key');
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/auth/login`);
      req.flush({
        userKey: mockAuthState.userKey,
        email: mockAuthState.email,
        role: mockAuthState.role,
        kurinKey: mockAuthState.kurinKey,
        tokens: { accessToken: mockAuthState.accessToken }
      });
    });

    it('should set kurin key to null', (done) => {
      const mockAuthState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      service.login({ email: 'test@example.com', password: 'password' }).subscribe(() => {
        service.setKurinKey(null);

        const updatedState = service.getAuthStateValue();
        expect(updatedState?.kurinKey).toBeNull();
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/auth/login`);
      req.flush({
        userKey: mockAuthState.userKey,
        email: mockAuthState.email,
        role: mockAuthState.role,
        kurinKey: mockAuthState.kurinKey,
        tokens: { accessToken: mockAuthState.accessToken }
      });
    });

    it('should not update state if no auth state exists', () => {
      service.setKurinKey('new-kurin-key');
      expect(service.getAuthStateValue()).toBeNull();
    });
  });

  describe('clearKurinKey', () => {
    it('should clear kurin key from auth state', (done) => {
      const mockAuthState: AuthState = {
        userKey: 'user-123',
        email: 'test@example.com',
        role: 'Manager',
        kurinKey: 'kurin-456',
        accessToken: 'token-789'
      };

      service.login({ email: 'test@example.com', password: 'password' }).subscribe(() => {
        service.clearKurinKey();

        const updatedState = service.getAuthStateValue();
        expect(updatedState?.kurinKey).toBeNull();
        done();
      });

      const req = httpMock.expectOne(`${apiUrl}/auth/login`);
      req.flush({
        userKey: mockAuthState.userKey,
        email: mockAuthState.email,
        role: mockAuthState.role,
        kurinKey: mockAuthState.kurinKey,
        tokens: { accessToken: mockAuthState.accessToken }
      });
    });

    it('should not affect state if no auth state exists', () => {
      service.clearKurinKey();
      expect(service.getAuthStateValue()).toBeNull();
    });
  });
});