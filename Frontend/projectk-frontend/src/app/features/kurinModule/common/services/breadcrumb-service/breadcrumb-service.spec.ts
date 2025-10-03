import { TestBed } from '@angular/core/testing';
import { BreadcrumbService } from './breadcrumb-service';
import { ActivatedRoute, ActivatedRouteSnapshot, NavigationEnd, Route, Router } from '@angular/router';
import { Subject } from 'rxjs';
import { MenuItem } from 'primeng/api';

describe('BreadcrumbService', () => {
  let service: BreadcrumbService;
  let routerEventsSubject: Subject<NavigationEnd>;
  let mockRouter: jasmine.SpyObj<Router>;
  let mockActivatedRoute: jasmine.SpyObj<ActivatedRoute>;

  const createActivatedRouteSnapshot = (data: Record<string, unknown>, params: Record<string, string>): ActivatedRouteSnapshot => {
    return {
      data,
      params,
      paramMap: jasmine.createSpyObj('ParamMap', ['get', 'has']),
      queryParamMap: jasmine.createSpyObj('ParamMap', ['get', 'has']),
      queryParams: {},
      url: [],
      outlet: 'primary',
      routeConfig: null,
      root: {} as ActivatedRouteSnapshot,
      parent: null,
      firstChild: null,
      children: [],
      pathFromRoot: [],
      fragment: null,
      title: undefined,
      component: null
    };
  };

  const createMockRouter = (url: string, config: Route[]): jasmine.SpyObj<Router> => {
    routerEventsSubject = new Subject<NavigationEnd>();
    const router = jasmine.createSpyObj<Router>('Router', ['navigate'], {
      events: routerEventsSubject.asObservable(),
      config: config
    });
    Object.defineProperty(router, 'url', {
      get: () => url,
      configurable: true
    });
    return router;
  };

  const createMockActivatedRoute = (snapshot: ActivatedRouteSnapshot, firstChild: ActivatedRoute | null = null): jasmine.SpyObj<ActivatedRoute> => {
    const route = jasmine.createSpyObj<ActivatedRoute>('ActivatedRoute', [], {
      snapshot,
      firstChild,
      children: []
    });
    return route;
  };

  afterEach(() => {
    if (routerEventsSubject) {
      routerEventsSubject.complete();
    }
  });

  it('should be created', () => {
    mockRouter = createMockRouter('/', []);
    const snapshot = createActivatedRouteSnapshot({}, {});
    mockActivatedRoute = createMockActivatedRoute(snapshot);

    TestBed.configureTestingModule({
      providers: [
        BreadcrumbService,
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    });

    service = TestBed.inject(BreadcrumbService);
    expect(service).toBeTruthy();
  });

  it('should emit empty breadcrumbs for route without breadcrumb data', (done) => {
    mockRouter = createMockRouter('/home', [
      { path: 'home' }
    ]);
    const snapshot = createActivatedRouteSnapshot({}, {});
    mockActivatedRoute = createMockActivatedRoute(snapshot);

    TestBed.configureTestingModule({
      providers: [
        BreadcrumbService,
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    });

    service = TestBed.inject(BreadcrumbService);

    let emissionCount = 0;
    service.breadcrumbs$.subscribe((breadcrumbs: MenuItem[]) => {
      emissionCount++;
      if (emissionCount === 2) {
        expect(breadcrumbs.length).toBe(0);
        done();
      }
    });

    routerEventsSubject.next(new NavigationEnd(1, '/home', '/home'));
  });

  it('should create single breadcrumb from route with breadcrumb data', (done) => {
    mockRouter = createMockRouter('/dashboard', [
      { path: 'dashboard', data: { breadcrumb: 'Dashboard' } }
    ]);
    const snapshot = createActivatedRouteSnapshot({ breadcrumb: 'Dashboard' }, {});
    mockActivatedRoute = createMockActivatedRoute(snapshot);

    TestBed.configureTestingModule({
      providers: [
        BreadcrumbService,
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    });

    service = TestBed.inject(BreadcrumbService);

    let emissionCount = 0;
    service.breadcrumbs$.subscribe((breadcrumbs: MenuItem[]) => {
      emissionCount++;
      if (emissionCount === 2) {
        expect(breadcrumbs.length).toBe(1);
        expect(breadcrumbs[0].label).toBe('Dashboard');
        expect(breadcrumbs[0].routerLink).toBe('/dashboard');
        done();
      }
    });

    routerEventsSubject.next(new NavigationEnd(1, '/dashboard', '/dashboard'));
  });

  it('should create breadcrumb with resolved route parameters', (done) => {
    mockRouter = createMockRouter('/users/123', [
      { path: 'users/:userId', data: { breadcrumb: 'User Profile' } }
    ]);
    const snapshot = createActivatedRouteSnapshot({ breadcrumb: 'User Profile' }, { userId: '123' });
    mockActivatedRoute = createMockActivatedRoute(snapshot);

    TestBed.configureTestingModule({
      providers: [
        BreadcrumbService,
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    });

    service = TestBed.inject(BreadcrumbService);

    let emissionCount = 0;
    service.breadcrumbs$.subscribe((breadcrumbs: MenuItem[]) => {
      emissionCount++;
      if (emissionCount === 2) {
        expect(breadcrumbs.length).toBe(1);
        expect(breadcrumbs[0].label).toBe('User Profile');
        expect(breadcrumbs[0].routerLink).toBe('/users/123');
        done();
      }
    });

    routerEventsSubject.next(new NavigationEnd(1, '/users/123', '/users/123'));
  });

  it('should create breadcrumb hierarchy with parent route', (done) => {
    mockRouter = createMockRouter('/settings/profile', [
      { path: 'settings', data: { breadcrumb: 'Settings' } },
      { path: 'settings/profile', data: { breadcrumb: 'Profile', parent: 'settings' } }
    ]);
    const snapshot = createActivatedRouteSnapshot({ breadcrumb: 'Profile', parent: 'settings' }, {});
    mockActivatedRoute = createMockActivatedRoute(snapshot);

    TestBed.configureTestingModule({
      providers: [
        BreadcrumbService,
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    });

    service = TestBed.inject(BreadcrumbService);

    let emissionCount = 0;
    service.breadcrumbs$.subscribe((breadcrumbs: MenuItem[]) => {
      emissionCount++;
      if (emissionCount === 2) {
        expect(breadcrumbs.length).toBe(2);
        expect(breadcrumbs[0].label).toBe('Settings');
        expect(breadcrumbs[0].routerLink).toBe('settings');
        expect(breadcrumbs[1].label).toBe('Profile');
        expect(breadcrumbs[1].routerLink).toBe('/settings/profile');
        done();
      }
    });

    routerEventsSubject.next(new NavigationEnd(1, '/settings/profile', '/settings/profile'));
  });

  it('should resolve parameters in parent path', (done) => {
    mockRouter = createMockRouter('/projects/42/tasks/99', [
      { path: 'projects/:projectId', data: { breadcrumb: 'Project Details' } },
      { path: 'projects/:projectId/tasks/:taskId', data: { breadcrumb: 'Task Details', parent: 'projects/:projectId' } }
    ]);
    const snapshot = createActivatedRouteSnapshot(
      { breadcrumb: 'Task Details', parent: 'projects/:projectId' },
      { projectId: '42', taskId: '99' }
    );
    mockActivatedRoute = createMockActivatedRoute(snapshot);

    TestBed.configureTestingModule({
      providers: [
        BreadcrumbService,
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    });

    service = TestBed.inject(BreadcrumbService);

    let emissionCount = 0;
    service.breadcrumbs$.subscribe((breadcrumbs: MenuItem[]) => {
      emissionCount++;
      if (emissionCount === 2) {
        expect(breadcrumbs.length).toBe(2);
        expect(breadcrumbs[0].label).toBe('Project Details');
        expect(breadcrumbs[0].routerLink).toBe('projects/42');
        expect(breadcrumbs[1].label).toBe('Task Details');
        expect(breadcrumbs[1].routerLink).toBe('/projects/42/tasks/99');
        done();
      }
    });

    routerEventsSubject.next(new NavigationEnd(1, '/projects/42/tasks/99', '/projects/42/tasks/99'));
  });

  it('should handle multiple route parameters', (done) => {
    mockRouter = createMockRouter('/org/10/team/20/member/30', [
      { path: 'org/:orgId/team/:teamId/member/:memberId', data: { breadcrumb: 'Member Details' } }
    ]);
    const snapshot = createActivatedRouteSnapshot(
      { breadcrumb: 'Member Details' },
      { orgId: '10', teamId: '20', memberId: '30' }
    );
    mockActivatedRoute = createMockActivatedRoute(snapshot);

    TestBed.configureTestingModule({
      providers: [
        BreadcrumbService,
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    });

    service = TestBed.inject(BreadcrumbService);

    let emissionCount = 0;
    service.breadcrumbs$.subscribe((breadcrumbs: MenuItem[]) => {
      emissionCount++;
      if (emissionCount === 2) {
        expect(breadcrumbs.length).toBe(1);
        expect(breadcrumbs[0].label).toBe('Member Details');
        expect(breadcrumbs[0].routerLink).toBe('/org/10/team/20/member/30');
        done();
      }
    });

    routerEventsSubject.next(new NavigationEnd(1, '/org/10/team/20/member/30', '/org/10/team/20/member/30'));
  });

  it('should create multi-level breadcrumb hierarchy', (done) => {
    mockRouter = createMockRouter('/level1/level2/level3', [
      { path: 'level1', data: { breadcrumb: 'Level 1' } },
      { path: 'level1/level2', data: { breadcrumb: 'Level 2', parent: 'level1' } },
      { path: 'level1/level2/level3', data: { breadcrumb: 'Level 3', parent: 'level1/level2' } }
    ]);
    const snapshot = createActivatedRouteSnapshot(
      { breadcrumb: 'Level 3', parent: 'level1/level2' },
      {}
    );
    mockActivatedRoute = createMockActivatedRoute(snapshot);

    TestBed.configureTestingModule({
      providers: [
        BreadcrumbService,
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    });

    service = TestBed.inject(BreadcrumbService);

    let emissionCount = 0;
    service.breadcrumbs$.subscribe((breadcrumbs: MenuItem[]) => {
      emissionCount++;
      if (emissionCount === 2) {
        expect(breadcrumbs.length).toBe(3);
        expect(breadcrumbs[0].label).toBe('Level 1');
        expect(breadcrumbs[0].routerLink).toBe('level1');
        expect(breadcrumbs[1].label).toBe('Level 2');
        expect(breadcrumbs[1].routerLink).toBe('level1/level2');
        expect(breadcrumbs[2].label).toBe('Level 3');
        expect(breadcrumbs[2].routerLink).toBe('/level1/level2/level3');
        done();
      }
    });

    routerEventsSubject.next(new NavigationEnd(1, '/level1/level2/level3', '/level1/level2/level3'));
  });

  it('should handle child routes in activated route tree', (done) => {
    mockRouter = createMockRouter('/parent/child/555', [
      { path: 'parent/child/:childId', data: { breadcrumb: 'Child Page' } }
    ]);
    
    const parentSnapshot = createActivatedRouteSnapshot({}, {});
    const childSnapshot = createActivatedRouteSnapshot({ breadcrumb: 'Child Page' }, { childId: '555' });
    const childRoute = createMockActivatedRoute(childSnapshot);
    mockActivatedRoute = createMockActivatedRoute(parentSnapshot, childRoute);

    TestBed.configureTestingModule({
      providers: [
        BreadcrumbService,
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    });

    service = TestBed.inject(BreadcrumbService);

    let emissionCount = 0;
    service.breadcrumbs$.subscribe((breadcrumbs: MenuItem[]) => {
      emissionCount++;
      if (emissionCount === 2) {
        expect(breadcrumbs.length).toBe(1);
        expect(breadcrumbs[0].label).toBe('Child Page');
        expect(breadcrumbs[0].routerLink).toBe('/parent/child/555');
        done();
      }
    });

    routerEventsSubject.next(new NavigationEnd(1, '/parent/child/555', '/parent/child/555'));
  });

  it('should cache and reuse parameters across navigation', (done) => {
    mockRouter = createMockRouter('/category/electronics/product/laptop', [
      { path: 'category/:categoryId', data: { breadcrumb: 'Category' } },
      { path: 'category/:categoryId/product/:productId', data: { breadcrumb: 'Product', parent: 'category/:categoryId' } }
    ]);
    const snapshot = createActivatedRouteSnapshot(
      { breadcrumb: 'Product', parent: 'category/:categoryId' },
      { categoryId: 'electronics', productId: 'laptop' }
    );
    mockActivatedRoute = createMockActivatedRoute(snapshot);

    TestBed.configureTestingModule({
      providers: [
        BreadcrumbService,
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    });

    service = TestBed.inject(BreadcrumbService);

    let emissionCount = 0;
    service.breadcrumbs$.subscribe((breadcrumbs: MenuItem[]) => {
      emissionCount++;
      if (emissionCount === 2) {
        expect(breadcrumbs.length).toBe(2);
        expect(breadcrumbs[0].label).toBe('Category');
        expect(breadcrumbs[0].routerLink).toBe('category/electronics');
        expect(breadcrumbs[1].label).toBe('Product');
        expect(breadcrumbs[1].routerLink).toBe('/category/electronics/product/laptop');
        done();
      }
    });

    routerEventsSubject.next(new NavigationEnd(1, '/category/electronics/product/laptop', '/category/electronics/product/laptop'));
  });

  it('should handle complex nested route structure with multiple children', (done) => {
    mockRouter = createMockRouter('/admin/users/123/edit', [
      { path: 'admin', data: { breadcrumb: 'Admin' } },
      { path: 'admin/users/:userId', data: { breadcrumb: 'User Details', parent: 'admin' } },
      { path: 'admin/users/:userId/edit', data: { breadcrumb: 'Edit User', parent: 'admin/users/:userId' } }
    ]);
    const snapshot = createActivatedRouteSnapshot(
      { breadcrumb: 'Edit User', parent: 'admin/users/:userId' },
      { userId: '123' }
    );
    mockActivatedRoute = createMockActivatedRoute(snapshot);

    TestBed.configureTestingModule({
      providers: [
        BreadcrumbService,
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    });

    service = TestBed.inject(BreadcrumbService);

    let emissionCount = 0;
    service.breadcrumbs$.subscribe((breadcrumbs: MenuItem[]) => {
      emissionCount++;
      if (emissionCount === 2) {
        expect(breadcrumbs.length).toBe(3);
        expect(breadcrumbs[0].label).toBe('Admin');
        expect(breadcrumbs[1].label).toBe('User Details');
        expect(breadcrumbs[1].routerLink).toBe('admin/users/123');
        expect(breadcrumbs[2].label).toBe('Edit User');
        done();
      }
    });

    routerEventsSubject.next(new NavigationEnd(1, '/admin/users/123/edit', '/admin/users/123/edit'));
  });
});