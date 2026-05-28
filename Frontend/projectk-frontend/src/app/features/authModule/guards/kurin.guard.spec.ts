import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { AuthState } from '../models/auth-state.model';
import { AuthService } from '../services/authService/auth.service';
import { PermissionService } from '../services/permission.service';
import { kurinAccessGuard } from './kurin.guard';

describe('kurinAccessGuard', () => {
  let authService: jasmine.SpyObj<AuthService>;
  let permissionService: jasmine.SpyObj<PermissionService>;
  let router: jasmine.SpyObj<Router>;

  beforeEach(() => {
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['getAuthStateValue']);
    permissionService = jasmine.createSpyObj<PermissionService>('PermissionService', [
      'isAdmin',
      'getRole',
      'canManagePlanning'
    ]);
    router = jasmine.createSpyObj<Router>('Router', ['createUrlTree']);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: PermissionService, useValue: permissionService },
        { provide: Router, useValue: router }
      ]
    });
  });

  it('redirects panel route to active kurin even for admin', () => {
    const tree = { target: '/kurin' };
    authService.getAuthStateValue.and.returnValue(createAuthState({ role: 'Admin', kurinKey: 'kurin-123' }));
    permissionService.isAdmin.and.returnValue(true);
    router.createUrlTree.and.returnValue(tree as never);

    const result = runGuard('panel');

    expect(router.createUrlTree).toHaveBeenCalledWith(['/kurin']);
    expect(result).toBe(tree as never);
  });

  it('allows panel route when admin has no active kurin', () => {
    authService.getAuthStateValue.and.returnValue(createAuthState({ role: 'Admin', kurinKey: null }));
    permissionService.isAdmin.and.returnValue(true);

    const result = runGuard('panel');

    expect(result).toBeTrue();
    expect(router.createUrlTree).not.toHaveBeenCalled();
  });

  it('redirects kurin route to panel for admin without active kurin', () => {
    const tree = { target: '/panel' };
    authService.getAuthStateValue.and.returnValue(createAuthState({ role: 'Admin', kurinKey: null }));
    permissionService.isAdmin.and.returnValue(true);
    router.createUrlTree.and.returnValue(tree as never);

    const result = runGuard('kurin');

    expect(router.createUrlTree).toHaveBeenCalledWith(['/panel']);
    expect(result).toBe(tree as never);
  });

  function runGuard(resource: string): unknown {
    return TestBed.runInInjectionContext(() => kurinAccessGuard(resource)({} as never, {} as never));
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
