import { inject, Injectable } from "@angular/core";
import { BehaviorSubject, map, Observable, tap } from "rxjs";
import { HttpClient } from "@angular/common/http";
import { environment } from "../../../environments/environment";
import { LoginRequest } from "../../models/login-request.model";
import { LoginResponse } from "../../models/login-response.model";
import { AuthState } from "../../models/auth-state.model";
import { KurinDto } from "../../../kurinModule/common/models/kurinDto";

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = environment.apiUrl;
  private readonly authState$ = new BehaviorSubject<AuthState | null>(null);
  private readonly http = inject(HttpClient);

  constructor() {
    const savedState = localStorage.getItem('authState');
    if (savedState) {
        this.authState$.next(JSON.parse(savedState));
    }
}

  getAuthState() {
    return this.authState$.asObservable();
  }

  getAuthStateValue() {
    return this.authState$.value;
  } 

  login(credentials: LoginRequest): Observable<AuthState> {
    return this.http.post<LoginResponse>(
      `${this.apiUrl}/auth/login`,
      credentials,
      { withCredentials: true }
    ).pipe(
      map(response => ({
        userKey: response.userKey,
        email: response.email,
        role: response.role,
        kurinKey: response.kurinKey,
        accessToken: response.tokens.accessToken,
      })),
      tap(state => {
        this.authState$.next(state);
        localStorage.setItem('authState', JSON.stringify(state));
      })
    );
  }

  logout() {
    return this.http.post(`${this.apiUrl}/auth/logout`, {}, { withCredentials: true, responseType: 'text' }).pipe(
      tap(() => {
        this.authState$.next(null);
        localStorage.removeItem('authState');
      })
    );
  }

  refreshToken(): Observable<string> {
    return this.http.post<{ accessToken: string }>(
      `${this.apiUrl}/auth/refresh`,
      {},
      { withCredentials: true }
    ).pipe(
      tap(res => {
        const state = this.authState$.value;
        if (state) {
          const newState = { ...state, accessToken: res.accessToken };
          this.authState$.next(newState);
          localStorage.setItem('authState', JSON.stringify(newState));
        }
      }),
      map(res => res.accessToken)
    );
  }

  registerFirstManager(kurinDto: KurinDto): Observable<void> {
    const body = {
      email: kurinDto.managerEmail,
      password: null,
      firstName: null,
      lastName: null,
      phoneNumber: null,
      kurinNumber: kurinDto.number,
      role: "Manager"
    };

    return this.http.post<void>(
      `${this.apiUrl}/auth/register/manager`,
      body,
      { withCredentials: true }
    );
  }

  getAccessToken(): string | null {
    return this.authState$.value?.accessToken ?? null;
  }

  setKurinKey(kurinKey: string | null): void {
    const state = this.authState$.value;
    if (state) {
      const newState = { ...state, kurinKey };
      this.authState$.next(newState);
      localStorage.setItem('authState', JSON.stringify(newState));
    }
  }

  clearKurinKey(): void {
    this.setKurinKey(null);
  }
}
