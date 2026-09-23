import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../../../environments/environment';
import { AuthService } from '../auth-service/auth.service';
import { LoginResponse } from '../../models/login-response.model';

/** The three chairs the demo offers; the names are what the API's `DemoSeat` is called. */
export type DemoSeat = 'Zvyazkovyi' | 'Vykhovnyk' | 'Youth';

/**
 * The public demo's front door. The API answers only in the Demo environment; everywhere else the
 * route does not exist, and the login page does not offer the buttons either.
 */
@Injectable({ providedIn: 'root' })
export class DemoService {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(AuthService);

  /** Whether this build is talking to a demo stand, as told by env.js at runtime. */
  static isDemo(): boolean {
    return /^demo$/i.test(environment.envName);
  }

  enter(seat: DemoSeat): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/demo/login`, { seat }, { withCredentials: true }).pipe(
      tap(response => this.auth.applyLoginResponse(response))
    );
  }
}
