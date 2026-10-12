import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, convertToParamMap, Router } from '@angular/router';
import { firstValueFrom, isObservable, of } from 'rxjs';
import { AuthState } from '../models/auth-state.model';
import { AuthService } from '../services/auth-service/auth.service';
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

  it('redirects authenticated admins with active kurin and no card to kurin page', async () => {
    const tree = { target: '/kurin' };
    const state = createAuthState({ isAdmin: true, permissions: [], roles: ['Admin'], memberKey: null, kurinKey: 'kurin-123' });
    authService.getAuthState.and.returnValue(of(state));
    authService.getAuthStateValue.and.returnValue(state);
    authService.ensureAccessToken.and.returnValue(of(true));
    router.createUrlTree.and.returnValue(tree as never);

    const result = await runGuard();

    expect(router.createUrlTree).toHaveBeenCalledWith(['/kurin']);
    expect(result).toBe(tree as never);
  });

  it('redirects authenticated admins without a kurin or a card to admin panel', async () => {
    const tree = { target: '/panel' };
    const state = createAuthState({ isAdmin: true, permissions: [], roles: ['Admin'], memberKey: null, kurinKey: null });
    authService.getAuthState.and.returnValue(of(state));
    authService.getAuthStateValue.and.returnValue(state);
    authService.ensureAccessToken.and.returnValue(of(true));
    router.createUrlTree.and.returnValue(tree as never);

    const result = await runGuard();

    expect(router.createUrlTree).toHaveBeenCalledWith(['/panel']);
    expect(result).toBe(tree as never);
  });

  it('redirects authenticated people with a card to the dashboard', async () => {
    const tree = { target: '/' };
    const state = createAuthState({ isAdmin: false, permissions: ['Group:Manage:KurinWide', 'Group:Update:KurinWide', 'Kurin:Update:KurinWide', 'Leadership:Manage:KurinWide', 'PlanningSession:Manage:KurinWide'], roles: ['KV.Zvyazkovyi'], kurinKey: 'kurin-123' });
    authService.getAuthState.and.returnValue(of(state));
    authService.getAuthStateValue.and.returnValue(state);
    authService.ensureAccessToken.and.returnValue(of(true));
    router.createUrlTree.and.returnValue(tree as never);

    const result = await runGuard();

    expect(router.createUrlTree).toHaveBeenCalledWith(['/']);
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

  it('lets a signed-in person open the sign-in page to add a second account', async () => {
    authService.getAuthState.and.returnValue(of(createAuthState()));

    const result = await runGuard({ add: '1' });

    expect(result).toBeTrue();
    expect(authService.ensureAccessToken).not.toHaveBeenCalled();
  });

  async function runGuard(queryParams: Record<string, string> = {}): Promise<unknown> {
    const route = { queryParamMap: convertToParamMap(queryParams) } as ActivatedRouteSnapshot;
    const result = TestBed.runInInjectionContext(() => publicAuthRedirectGuard(route, {} as never));
    return isObservable(result) ? firstValueFrom(result) : result;
  }

  function createAuthState(overrides: Partial<AuthState> = {}): AuthState {
    return {
      userKey: 'user-123',
      memberKey: 'member-123',
      email: 'test@example.com',
      isAdmin: false, permissions: ['Group:Manage:KurinWide', 'Group:Update:KurinWide', 'Kurin:Update:KurinWide', 'Leadership:Manage:KurinWide', 'PlanningSession:Manage:KurinWide'], roles: ['KV.Zvyazkovyi'],
      kurinKey: 'kurin-123',
      accessToken: 'token-123',
      ...overrides
    };
  }
});
