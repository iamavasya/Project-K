import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { Observable, of } from 'rxjs';
import { AuthService } from '../services/authService/auth.service';
import { capabilityGuard } from './capability.guard';

describe('capabilityGuard', () => {
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;
  let forbiddenTree: UrlTree;

  beforeEach(() => {
    forbiddenTree = {} as UrlTree;
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['getAuthState', 'getAuthStateValue']);
    router = jasmine.createSpyObj<Router>('Router', ['createUrlTree']);
    router.createUrlTree.and.returnValue(forbiddenTree);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router }
      ]
    });
  });

  function runGuard(capabilities: Parameters<typeof capabilityGuard>): Observable<boolean | UrlTree> {
    return capabilityGuard(...capabilities)(
      {} as ActivatedRouteSnapshot,
      {} as RouterStateSnapshot
    ) as Observable<boolean | UrlTree>;
  }

  it('allows any configured role', (done) => {
    authService.getAuthState.and.returnValue(of({
      userKey: 'user-1',
      memberKey: null,
      email: 'admin@example.com',
      isAdmin: true, permissions: [], roles: ['Admin'],
      kurinKey: 'kurin-1',
      accessToken: 'token'
    }));
    authService.getAuthStateValue.and.returnValue({ userKey: 'user-1', memberKey: null, email: 'admin@example.com', isAdmin: true, permissions: [], roles: ['Admin'], kurinKey: 'kurin-1', accessToken: 'token' } as never);

    TestBed.runInInjectionContext(() => {
      const result$ = runGuard(['admin', 'kurinManagement']);

      result$.subscribe(result => {
        expect(result).toBeTrue();
        done();
      });
    });
  });

  it('redirects when the current role is not configured', (done) => {
    authService.getAuthState.and.returnValue(of({
      userKey: 'user-1',
      memberKey: null,
      email: 'mentor@example.com',
      isAdmin: false, permissions: ['Group:Update:OwnGroups'], roles: ['Group.Hurtkoviy'],
      kurinKey: 'kurin-1',
      accessToken: 'token'
    }));
    authService.getAuthStateValue.and.returnValue({ userKey: 'user-1', memberKey: null, email: 'mentor@example.com', isAdmin: false, permissions: ['Group:Update:OwnGroups'], roles: ['Group.Hurtkoviy'], kurinKey: 'kurin-1', accessToken: 'token' } as never);

    TestBed.runInInjectionContext(() => {
      const result$ = runGuard(['admin', 'kurinManagement']);

      result$.subscribe(result => {
        expect(result).toBe(forbiddenTree);
        expect(router.createUrlTree).toHaveBeenCalledWith(['/forbidden']);
        done();
      });
    });
  });
});
