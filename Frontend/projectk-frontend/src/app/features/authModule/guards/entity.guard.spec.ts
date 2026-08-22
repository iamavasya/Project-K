import { TestBed } from '@angular/core/testing';
import { HttpErrorResponse, provideHttpClient, withXhr } from '@angular/common/http';
import { ActivatedRouteSnapshot, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { EntityGuard } from './entity.guard';
import { EntityService } from '../services/entity.service';

describe('EntityGuard', () => {
  let guard: EntityGuard;
  let entityService: jasmine.SpyObj<EntityService>;
  let router: jasmine.SpyObj<Router>;

  function routeFor(entityType: string | null, entityKey: string | null): ActivatedRouteSnapshot {
    return {
      data: entityType ? { entityType } : {},
      paramMap: convertToParamMap(entityKey && entityType ? { [`${entityType}Key`]: entityKey } : {})
    } as unknown as ActivatedRouteSnapshot;
  }

  beforeEach(() => {
    entityService = jasmine.createSpyObj('EntityService', ['checkEntityAccess']);
    router = jasmine.createSpyObj('Router', ['navigate']);

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withXhr()),
        { provide: EntityService, useValue: entityService },
        { provide: Router, useValue: router }
      ]
    });

    guard = TestBed.inject(EntityGuard);
  });

  it('allows navigation when the route carries no entity to check', done => {
    guard.canActivate(routeFor(null, null)).subscribe(allowed => {
      expect(allowed).toBeTrue();
      expect(entityService.checkEntityAccess).not.toHaveBeenCalled();
      done();
    });
  });

  it('allows navigation when access is granted', done => {
    entityService.checkEntityAccess.and.returnValue(of(true));

    guard.canActivate(routeFor('member', 'member-1')).subscribe(allowed => {
      expect(allowed).toBeTrue();
      expect(router.navigate).not.toHaveBeenCalled();
      done();
    });
  });

  it('redirects to forbidden when access is denied', done => {
    entityService.checkEntityAccess.and.returnValue(of(false));

    guard.canActivate(routeFor('member', 'member-1')).subscribe(allowed => {
      expect(allowed).toBeFalse();
      expect(router.navigate).toHaveBeenCalledWith(['/forbidden']);
      done();
    });
  });

  it('redirects to forbidden on a cross-kurin 403', done => {
    entityService.checkEntityAccess.and.returnValue(throwError(() =>
      new HttpErrorResponse({ status: 403, error: { message: 'Resource belongs to a different kurin scope.' } })));

    guard.canActivate(routeFor('member', 'member-1')).subscribe(allowed => {
      expect(allowed).toBeFalse();
      expect(router.navigate).toHaveBeenCalledWith(['/forbidden']);
      done();
    });
  });

  it('lets an MFA challenge through so its dialog can appear', done => {
    entityService.checkEntityAccess.and.returnValue(throwError(() =>
      new HttpErrorResponse({ status: 403, error: { message: 'MFA is required for this operation.' } })));

    guard.canActivate(routeFor('member', 'member-1')).subscribe(allowed => {
      expect(allowed).toBeTrue();
      expect(router.navigate).not.toHaveBeenCalled();
      done();
    });
  });

  it('lets navigation through when the server was never reached', done => {
    entityService.checkEntityAccess.and.returnValue(throwError(() =>
      new HttpErrorResponse({ status: 0 })));

    guard.canActivate(routeFor('member', 'member-1')).subscribe(allowed => {
      expect(allowed).toBeTrue();
      done();
    });
  });

  it('blocks navigation when the server answers with an error it cannot read', done => {
    entityService.checkEntityAccess.and.returnValue(throwError(() =>
      new HttpErrorResponse({ status: 500 })));

    guard.canActivate(routeFor('member', 'member-1')).subscribe(allowed => {
      expect(allowed).toBeFalse();
      expect(router.navigate).toHaveBeenCalledWith(['/forbidden']);
      done();
    });
  });
});
