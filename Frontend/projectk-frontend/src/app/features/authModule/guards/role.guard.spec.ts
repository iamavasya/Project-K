import { TestBed } from '@angular/core/testing';
import { Router, UrlTree } from '@angular/router';
import { Observable, of } from 'rxjs';
import { AuthService } from '../services/authService/auth.service';
import { roleGuard } from './role.guard';

describe('roleGuard', () => {
  let authService: jasmine.SpyObj<AuthService>;
  let router: jasmine.SpyObj<Router>;
  let forbiddenTree: UrlTree;

  beforeEach(() => {
    forbiddenTree = {} as UrlTree;
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['getAuthState']);
    router = jasmine.createSpyObj<Router>('Router', ['createUrlTree']);
    router.createUrlTree.and.returnValue(forbiddenTree);

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router }
      ]
    });
  });

  it('allows any configured role', (done) => {
    authService.getAuthState.and.returnValue(of({
      userKey: 'user-1',
      memberKey: null,
      email: 'admin@example.com',
      role: 'Admin',
      kurinKey: 'kurin-1',
      accessToken: 'token'
    }));

    TestBed.runInInjectionContext(() => {
      const result$ = roleGuard('Admin', 'Manager')({} as any, {} as any) as Observable<boolean | UrlTree>;

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
      role: 'Mentor',
      kurinKey: 'kurin-1',
      accessToken: 'token'
    }));

    TestBed.runInInjectionContext(() => {
      const result$ = roleGuard('Admin', 'Manager')({} as any, {} as any) as Observable<boolean | UrlTree>;

      result$.subscribe(result => {
        expect(result).toBe(forbiddenTree);
        expect(router.createUrlTree).toHaveBeenCalledWith(['/forbidden']);
        done();
      });
    });
  });
});
