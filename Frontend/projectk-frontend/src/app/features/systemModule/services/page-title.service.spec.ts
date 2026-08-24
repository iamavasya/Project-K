import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, TitleStrategy, provideRouter } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';
import { provideHttpClient } from '@angular/common/http';
import { PageTitleService } from './page-title.service';
import { ProjectKTitleStrategy } from './page-title.strategy';
import { AuthService } from '../../authModule/services/authService/auth.service';
import { KurinService } from '../../kurinModule/common/services/kurin-service/kurin.service';
import { GroupService } from '../../kurinModule/common/services/group-service/group.service';
import { MemberService } from '../../kurinModule/common/services/member-service/member.service';
import { KurinDto } from '../../kurinModule/common/models/kurinDto';
import { GroupDto } from '../../kurinModule/common/models/groupDto';
import { MemberDto } from '../../kurinModule/common/models/memberDto';
import { environment } from '../../../../environments/environment';

const APP_NAME = environment.appName;

interface RouteLevel {
  params?: Record<string, string>;
  data?: Record<string, unknown>;
}

const createState = (levels: RouteLevel[]): RouterStateSnapshot => {
  const snapshots = levels.map(level => ({
    params: level.params ?? {},
    data: level.data ?? {},
    firstChild: null
  })) as unknown as ActivatedRouteSnapshot[];

  snapshots.forEach((snapshot, index) => {
    (snapshot as { firstChild: ActivatedRouteSnapshot | null }).firstChild =
      snapshots[index + 1] ?? null;
  });

  return { root: snapshots[0] } as RouterStateSnapshot;
};

describe('PageTitleService', () => {
  let service: PageTitleService;
  let title: jasmine.SpyObj<Title>;
  let authService: jasmine.SpyObj<AuthService>;
  let kurinService: jasmine.SpyObj<KurinService>;
  let groupService: jasmine.SpyObj<GroupService>;
  let memberService: jasmine.SpyObj<MemberService>;

  const currentTitle = (): string => title.setTitle.calls.mostRecent().args[0];

  beforeEach(() => {
    title = jasmine.createSpyObj<Title>('Title', ['setTitle', 'getTitle']);
    authService = jasmine.createSpyObj<AuthService>('AuthService', ['getAuthStateValue']);
    kurinService = jasmine.createSpyObj<KurinService>('KurinService', ['getByKey']);
    groupService = jasmine.createSpyObj<GroupService>('GroupService', ['getByKey']);
    memberService = jasmine.createSpyObj<MemberService>('MemberService', ['getByKey']);

    TestBed.configureTestingModule({
      providers: [
        PageTitleService,
        { provide: Title, useValue: title },
        { provide: AuthService, useValue: authService },
        { provide: KurinService, useValue: kurinService },
        { provide: GroupService, useValue: groupService },
        { provide: MemberService, useValue: memberService }
      ]
    });

    service = TestBed.inject(PageTitleService);
  });

  it('shows the app name alone for routes without a title', () => {
    service.applyRouteState(null, createState([{}]));

    expect(currentTitle()).toBe(APP_NAME);
  });

  it('appends the app name to a static route title', () => {
    service.applyRouteState('Користувачі', createState([{}]));

    expect(currentTitle()).toBe(`Користувачі · ${APP_NAME}`);
  });

  it('resolves the kurin number from the route param', () => {
    kurinService.getByKey.and.returnValue(of({ kurinKey: 'k-1', number: 12 } as KurinDto));

    service.applyRouteState('Курінь', createState([
      {},
      { params: { kurinKey: 'k-1' }, data: { titleContext: 'kurin' } }
    ]));

    expect(kurinService.getByKey).toHaveBeenCalledWith('k-1');
    expect(currentTitle()).toBe(`к. ч. 12 · ${APP_NAME}`);
  });

  it('falls back to the signed-in kurin when the route carries no key', () => {
    authService.getAuthStateValue.and.returnValue({ kurinKey: 'k-7' } as ReturnType<AuthService['getAuthStateValue']>);
    kurinService.getByKey.and.returnValue(of({ kurinKey: 'k-7', number: 7 } as KurinDto));

    service.applyRouteState('Курінь', createState([{}, { data: { titleContext: 'kurin' } }]));

    expect(kurinService.getByKey).toHaveBeenCalledWith('k-7');
    expect(currentTitle()).toBe(`к. ч. 7 · ${APP_NAME}`);
  });

  it('resolves the group name', () => {
    groupService.getByKey.and.returnValue(of({ groupKey: 'g-1', name: 'Соколи' } as GroupDto));

    service.applyRouteState('Гурток', createState([
      {},
      { params: { groupKey: 'g-1' }, data: { titleContext: 'group' } }
    ]));

    expect(currentTitle()).toBe(`г. Соколи · ${APP_NAME}`);
  });

  it('resolves the member as last name then first name', () => {
    memberService.getByKey.and.returnValue(of({
      memberKey: 'm-1',
      lastName: 'Муха',
      firstName: 'Ростислав'
    } as MemberDto));

    service.applyRouteState('Картка учасника', createState([
      {},
      { params: { memberKey: 'm-1' }, data: { titleContext: 'member' } }
    ]));

    expect(currentTitle()).toBe(`Муха Ростислав · ${APP_NAME}`);
  });

  it('keeps the route title when the entity cannot be loaded', () => {
    memberService.getByKey.and.returnValue(throwError(() => new Error('offline')));

    service.applyRouteState('Картка учасника', createState([
      {},
      { params: { memberKey: 'm-1' }, data: { titleContext: 'member' } }
    ]));

    expect(currentTitle()).toBe(`Картка учасника · ${APP_NAME}`);
  });

  it('drops the previous entity when the next navigation lands', () => {
    groupService.getByKey.and.returnValue(of({ groupKey: 'g-1', name: 'Соколи' } as GroupDto));
    service.applyRouteState('Гурток', createState([
      {},
      { params: { groupKey: 'g-1' }, data: { titleContext: 'group' } }
    ]));

    service.applyRouteState('Адміністрація', createState([{}]));

    expect(currentTitle()).toBe(`Адміністрація · ${APP_NAME}`);
  });

  it('ignores a resolve that lands after the user navigated away', () => {
    const slowGroup = new Subject<GroupDto>();
    groupService.getByKey.and.returnValue(slowGroup.asObservable());

    service.applyRouteState('Гурток', createState([
      {},
      { params: { groupKey: 'g-1' }, data: { titleContext: 'group' } }
    ]));

    service.applyRouteState('Адміністрація', createState([{}]));
    slowGroup.next({ groupKey: 'g-1', name: 'Соколи' } as GroupDto);

    expect(currentTitle()).toBe(`Адміністрація · ${APP_NAME}`);
  });

  it('lets a page publish its context directly', () => {
    service.applyRouteState('Гурток', createState([{}]));
    service.setContext('г. Беркути');

    expect(currentTitle()).toBe(`г. Беркути · ${APP_NAME}`);
  });
});

describe('ProjectKTitleStrategy wiring', () => {
  it('resolves through a real Router without closing a DI cycle', () => {
    // PageTitleService is built by the TitleStrategy the Router owns, so injecting the
    // Router into it would fail bootstrap with NG0200 — a stubbed Router hides that.
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideRouter([]),
        { provide: TitleStrategy, useClass: ProjectKTitleStrategy }
      ]
    });

    expect(() => TestBed.inject(Router)).not.toThrow();
    expect(TestBed.inject(TitleStrategy)).toBeInstanceOf(ProjectKTitleStrategy);
  });
});
