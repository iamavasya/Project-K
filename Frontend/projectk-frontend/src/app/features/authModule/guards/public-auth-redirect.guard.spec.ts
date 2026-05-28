import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { firstValueFrom, isObservable, of } from 'rxjs';
import { AuthState } from '../models/auth-state.model';
import { AuthService } from '../services/authService/auth.service';
import { publicAuthRedirectGuard } from './public-auth-redirect.guard';

describe('publicAuthRedirectGuard', () => {
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', [
      'getAuthState',
      'getAuthStateValue',
      'ensureAccessToken'
    ]);
    router = jasmine.createSpyObj<Router>('Router', ['createUrlTree']);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router }
      ]
    });
  });

  it('allows anonymous users to open public routes', async () => {
    authService.getAuthState.and.returnValue(of(null));

    const result = await runGuard();

    expect(result).toBeTrue();
    expect(authService.ensureAccessToken).not.toHaveBeenCalled();
  });

  it('redirects authenticated admins with active kurin to kurin page', async () => {
    const tree = { target: '/kurin' };
    const state = createAuthState({ role: 'Admin', kurinKey: 'kurin-123' });
    authService.getAuthState.and.returnValue(of(state));
    authService.getAuthStateValue.and.returnValue(state);
    authService.ensureAccessToken.and.returnValue(of(true));
    router.createUrlTree.and.returnValue(tree as never);

    const result = await runGuard();

    expect(router.createUrlTree).toHaveBeenCalledWith(['/kurin']);
    expect(result).toBe(tree as never);
  });

  it('redirects authenticated admins without active kurin to admin panel', async () => {
    const tree = { target: '/panel' };
    const state = createAuthState({ role: 'Admin', kurinKey: null });
    authService.getAuthState.and.returnValue(of(state));
    authService.getAuthStateValue.and.returnValue(state);
    authService.ensureAccessToken.and.returnValue(of(true));
    router.createUrlTree.and.returnValue(tree as never);

    const result = await runGuard();

    expect(router.createUrlTree).toHaveBeenCalledWith(['/panel']);
    expect(result).toBe(tree as never);
  });

  it('redirects authenticated non-admin users to their kurin page', async () => {
    const tree = { target: '/kurin' };
    const state = createAuthState({ role: 'Manager', kurinKey: 'kurin-123' });
    authService.getAuthState.and.returnValue(of(state));
    authService.getAuthStateValue.and.returnValue(state);
    authService.ensureAccessToken.and.returnValue(of(true));
    router.createUrlTree.and.returnValue(tree as never);

    const result = await runGuard();

    expect(router.createUrlTree).toHaveBeenCalledWith(['/kurin']);
    expect(result).toBe(tree as never);
  });

  it('allows public route when stored auth state cannot refresh token', async () => {
    const state = createAuthState();
    authService.getAuthState.and.returnValue(of(state));
    authService.getAuthStateValue.and.returnValue(null);
    authService.ensureAccessToken.and.returnValue(of(false));

    const result = await runGuard();

    expect(result).toBeTrue();
    expect(router.createUrlTree).not.toHaveBeenCalled();
  });

  async function runGuard(): Promise<unknown> {
    const result = TestBed.runInInjectionContext(() => publicAuthRedirectGuard({} as never, {} as never));
    return isObservable(result) ? firstValueFrom(result) : result;
  }

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
});
