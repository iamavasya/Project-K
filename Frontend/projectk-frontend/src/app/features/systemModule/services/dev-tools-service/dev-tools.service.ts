import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, Observable, of, switchMap, tap, throwError } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { AuthService } from '../../../authModule/services/auth-service/auth.service';
import { LoginResponse } from '../../../authModule/models/login-response.model';

/**
 * The seats the dev role switcher offers; the office values are what the API's `DevRole` is called,
 * and `Person` is one particular member, the one whose card is open.
 */
export type DevRole = 'Zvyazkovyi' | 'Vykhovnyk' | 'Kurinnyi' | 'Skarbnyk' | 'Member' | 'Person';

export interface DevImpersonationResponse {
  login: LoginResponse;
  returnTicket: string;
  role: DevRole;
  kurinKey: string;
}

const RETURN_TICKET_KEY = 'lileyka-dev-return-ticket';
const BORROWED_ROLE_KEY = 'lileyka-dev-borrowed-role';

/**
 * Talks to the local-tier dev tools. The API answers only on Development, E2E and Tailscale;
 * everywhere else these routes do not exist, and the switcher is not rendered either.
 */
@Injectable({ providedIn: 'root' })
export class DevToolsService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);
  private readonly apiUrl = `${environment.apiUrl}/dev`;

  /** The role currently borrowed, or null when the administrator is themselves. */
  borrowedRole(): DevRole | null {
    return (localStorage.getItem(BORROWED_ROLE_KEY) as DevRole | null) ?? null;
  }

  hasReturnTicket(): boolean {
    return !!localStorage.getItem(RETURN_TICKET_KEY);
  }

  /** Whoever holds the office in the kurin on screen. */
  impersonate(role: Exclude<DevRole, 'Person'>): Observable<DevImpersonationResponse> {
    return this.borrow(`${this.apiUrl}/impersonate`, { role });
  }

  /** One particular member: the one whose card is open. */
  impersonateMember(memberKey: string): Observable<DevImpersonationResponse> {
    return this.borrow(`${this.apiUrl}/impersonate/member`, { memberKey });
  }

  returnToAdmin(): Observable<LoginResponse> {
    const ticket = localStorage.getItem(RETURN_TICKET_KEY) ?? '';
    return this.http.post<LoginResponse>(`${this.apiUrl}/return`, { ticket }, { withCredentials: true }).pipe(
      tap(response => {
        this.forgetTicket();
        this.auth.applyLoginResponse(response);
      })
    );
  }

  forgetTicket(): void {
    localStorage.removeItem(RETURN_TICKET_KEY);
    localStorage.removeItem(BORROWED_ROLE_KEY);
  }

  /**
   * Only an administrator may borrow a seat, and a borrowed session is not one. Switching
   * straight from one seat to another therefore steps back to the administrator first, with
   * the ticket, and only then borrows again; one click for the tester, two calls underneath.
   */
  private borrow(url: string, body: object): Observable<DevImpersonationResponse> {
    return this.backToAdmin().pipe(
      switchMap(() => this.http.post<DevImpersonationResponse>(url, body, { withCredentials: true })),
      tap(response => {
        // The ticket outlives the borrowed session on purpose: it is the way back.
        localStorage.setItem(RETURN_TICKET_KEY, response.returnTicket);
        localStorage.setItem(BORROWED_ROLE_KEY, response.role);
        this.auth.applyLoginResponse(response.login);
      })
    );
  }

  /**
   * The ticket lives twelve hours; one left over from yesterday would fail every switch from
   * now on. A dead ticket (401) is dropped and the borrow goes ahead on the current session,
   * which either is the administrator's already or gets refused honestly by the API. Any other
   * failure is a blip: keep the ticket, report the error.
   */
  private backToAdmin(): Observable<unknown> {
    if (!this.hasReturnTicket() || this.auth.getAuthStateValue()?.isAdmin) {
      this.forgetTicket();
      return of(null);
    }

    return this.returnToAdmin().pipe(
      catchError((error: unknown) => {
        if ((error as { status?: number })?.status !== 401) {
          return throwError(() => error);
        }

        this.forgetTicket();
        return of(null);
      })
    );
  }
}
