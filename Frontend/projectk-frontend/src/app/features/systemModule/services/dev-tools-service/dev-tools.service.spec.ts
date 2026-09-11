import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { DevToolsService } from './dev-tools.service';
import { AuthService } from '../../../authModule/services/auth-service/auth.service';
import { environment } from '../../../../../environments/environment';
import { LoginResponse } from '../../../authModule/models/login-response.model';

describe('DevToolsService', () => {
  let service: DevToolsService;
  let http: HttpTestingController;
  let auth: jasmine.SpyObj<AuthService>;

  const login: LoginResponse = {
    userKey: 'zv-1', memberKey: 'm-1', email: 'zv@example.com', isAdmin: false,
    permissions: [], roles: ['KV.Zvyazkovyi'], kurinKey: 'k-1', requiresMfa: false,
    tokens: { accessToken: 'borrowed' }
  } as LoginResponse;

  beforeEach(() => {
    auth = jasmine.createSpyObj<AuthService>('AuthService', ['applyLoginResponse']);
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: AuthService, useValue: auth }]
    });
    service = TestBed.inject(DevToolsService);
    http = TestBed.inject(HttpTestingController);
    localStorage.clear();
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('borrows a seat, keeps the ticket back and takes the sign-in as the session', () => {
    service.impersonate('Zvyazkovyi').subscribe();

    const req = http.expectOne(`${environment.apiUrl}/dev/impersonate`);
    expect(req.request.body).toEqual({ role: 'Zvyazkovyi' });
    expect(req.request.withCredentials).toBeTrue();
    req.flush({ login, returnTicket: 'ticket-1', role: 'Zvyazkovyi', kurinKey: 'k-1' });

    expect(auth.applyLoginResponse).toHaveBeenCalledWith(login);
    expect(service.hasReturnTicket()).toBeTrue();
    expect(service.borrowedRole()).toBe('Zvyazkovyi');
  });

  it('steps back to the administrator first when a seat is already borrowed', () => {
    localStorage.setItem('lileyka-dev-return-ticket', 'ticket-1');
    localStorage.setItem('lileyka-dev-borrowed-role', 'Zvyazkovyi');

    service.impersonateMember('m-2').subscribe();

    const back = http.expectOne(`${environment.apiUrl}/dev/return`);
    expect(back.request.body).toEqual({ ticket: 'ticket-1' });
    http.expectNone(`${environment.apiUrl}/dev/impersonate/member`);
    back.flush({ ...login, userKey: 'admin-1', isAdmin: true });

    const borrow = http.expectOne(`${environment.apiUrl}/dev/impersonate/member`);
    borrow.flush({ login, returnTicket: 'ticket-2', role: 'Person', kurinKey: 'k-1' });

    expect(auth.applyLoginResponse).toHaveBeenCalledTimes(2);
    expect(localStorage.getItem('lileyka-dev-return-ticket')).toBe('ticket-2');
    expect(service.borrowedRole()).toBe('Person');
  });

  it('borrows one particular person by their member key', () => {
    service.impersonateMember('m-1').subscribe();

    const req = http.expectOne(`${environment.apiUrl}/dev/impersonate/member`);
    expect(req.request.body).toEqual({ memberKey: 'm-1' });
    req.flush({ login, returnTicket: 'ticket-2', role: 'Person', kurinKey: 'k-1' });

    expect(auth.applyLoginResponse).toHaveBeenCalledWith(login);
    expect(service.borrowedRole()).toBe('Person');
  });

  it('returns with the ticket and forgets it once the administrator is back', () => {
    localStorage.setItem('lileyka-dev-return-ticket', 'ticket-1');
    localStorage.setItem('lileyka-dev-borrowed-role', 'Member');

    service.returnToAdmin().subscribe();

    const req = http.expectOne(`${environment.apiUrl}/dev/return`);
    expect(req.request.body).toEqual({ ticket: 'ticket-1' });
    req.flush({ ...login, userKey: 'admin-1', isAdmin: true });

    expect(auth.applyLoginResponse).toHaveBeenCalled();
    expect(service.hasReturnTicket()).toBeFalse();
    expect(service.borrowedRole()).toBeNull();
  });

  it('keeps the ticket when the return call fails, so the way back is not lost on a blip', () => {
    localStorage.setItem('lileyka-dev-return-ticket', 'ticket-1');

    service.returnToAdmin().subscribe({ error: () => undefined });
    http.expectOne(`${environment.apiUrl}/dev/return`).flush('down', { status: 503, statusText: 'Unavailable' });

    expect(service.hasReturnTicket()).toBeTrue();
  });
});
