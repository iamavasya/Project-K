import { inject, Injectable } from "@angular/core";
import { BehaviorSubject, catchError, finalize, map, Observable, of, shareReplay, tap } from "rxjs";
import { HttpClient } from "@angular/common/http";
import { environment } from "../../../../../environments/environment";
import { LoginRequest } from "../../models/login-request.model";
import { LoginResponse } from "../../models/login-response.model";
import { AuthState } from "../../models/auth-state.model";
import { KurinDto } from "../../../kurinModule/common/models/kurinDto";
import { clearMfaSessionState } from "../mfa-session-state";

export interface MfaSetupResponse {
  sharedKey: string;
  authenticatorUri: string;
  qrCodeBase64: string;
}

export interface MfaStatusResponse {
  isMfaEnabled: boolean;
}

export interface MfaEnableResponse {
  enabled: boolean;
  recoveryCodes: string[];
}

export interface MfaRecoveryCodesResponse {
  recoveryCodes: string[];
}

export interface SetupStatusResponse {
  isInitialized: boolean;
}

export interface InitializeSetupRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  enforcePrivilegedMfa: boolean;
  seedDemoData: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = environment.apiUrl;
  private readonly authState$ = new BehaviorSubject<AuthState | null>(null);
  private readonly http = inject(HttpClient);
  private refreshTokenRequest$: Observable<string> | null = null;

  constructor() {
    const savedState = localStorage.getItem('authState');
    if (savedState) {
      this.authState$.next(this.deserializeStoredState(savedState));
    }
  }

  getAuthState() {
    return this.authState$.asObservable();
  }

  getAuthStateValue() {
    return this.authState$.value;
  } 

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(
      `${this.apiUrl}/auth/login`,
      credentials,
      { withCredentials: true }
    ).pipe(
      tap(response => {
        if (!response.requiresMfa && response.tokens) {
          clearMfaSessionState();
          const state: AuthState = {
            userKey: response.userKey,
            memberKey: response.memberKey,
            email: response.email,
            role: response.role,
            kurinKey: response.kurinKey,
            accessToken: response.tokens.accessToken,
          };
          this.authState$.next(state);
          this.persistAuthState(state);
        }
      })
    );
  }

  verifyMfaLogin(email: string, code: string): Observable<AuthState> {
    return this.http.post<LoginResponse>(
      `${this.apiUrl}/auth/mfa/login-verify`,
      { email, code, rememberMe: true },
      { withCredentials: true }
    ).pipe(
      map(response => {
        if (!response.tokens) throw new Error('No tokens in response');
        return {
          userKey: response.userKey,
          memberKey: response.memberKey,
          email: response.email,
          role: response.role,
          kurinKey: response.kurinKey,
          accessToken: response.tokens.accessToken,
        };
      }),
      tap(state => {
        clearMfaSessionState();
        this.authState$.next(state);
        this.persistAuthState(state);
      })
    );
  }

  getMfaSetup(): Observable<MfaSetupResponse> {
    return this.http.get<MfaSetupResponse>(
      `${this.apiUrl}/auth/mfa/setup`,
      { withCredentials: true }
    );
  }

  enableMfa(code: string): Observable<MfaEnableResponse> {
    return this.http.post<MfaEnableResponse>(
      `${this.apiUrl}/auth/mfa/enable`,
      { code },
      { withCredentials: true }
    );
  }

  rotateMfaRecoveryCodes(currentPassword: string): Observable<MfaRecoveryCodesResponse> {
    return this.http.post<MfaRecoveryCodesResponse>(
      `${this.apiUrl}/auth/mfa/recovery-codes`,
      { currentPassword },
      { withCredentials: true }
    );
  }

  getMfaStatus(): Observable<MfaStatusResponse> {
    return this.http.get<MfaStatusResponse>(
      `${this.apiUrl}/auth/mfa/status`,
      { withCredentials: true }
    );
  }

  getSetupStatus(): Observable<SetupStatusResponse> {
    return this.http.get<SetupStatusResponse>(
      `${this.apiUrl}/auth/setup/status`,
      { withCredentials: true }
    );
  }

  initializeSetup(command: InitializeSetupRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(
      `${this.apiUrl}/auth/setup/initialize`,
      command,
      { withCredentials: true }
    ).pipe(
      tap(response => {
        if (!response.requiresMfa && response.tokens) {
          clearMfaSessionState();
          const state: AuthState = {
            userKey: response.userKey,
            memberKey: response.memberKey,
            email: response.email,
            role: response.role,
            kurinKey: response.kurinKey,
            accessToken: response.tokens.accessToken,
          };
          this.authState$.next(state);
          this.persistAuthState(state);
        }
      })
    );
  }

  clearLocalState(): void {
    this.authState$.next(null);
    localStorage.removeItem('authState');
    clearMfaSessionState();
  }

  logout() {
    this.clearLocalState();
    return this.http.post(`${this.apiUrl}/auth/logout`, {}, { withCredentials: true, responseType: 'text' });
  }

  refreshToken(): Observable<string> {
    if (this.refreshTokenRequest$) {
      return this.refreshTokenRequest$;
    }

    this.refreshTokenRequest$ = this.http.post<{ accessToken: string }>(
      `${this.apiUrl}/auth/refresh`,
      {},
      { withCredentials: true }
    ).pipe(
      tap(res => {
        const state = this.authState$.value;
        if (state) {
          const newState = { ...state, accessToken: res.accessToken };
          this.authState$.next(newState);
          this.persistAuthState(newState);
        }
      }),
      map(res => res.accessToken),
      finalize(() => {
        this.refreshTokenRequest$ = null;
      }),
      shareReplay({ bufferSize: 1, refCount: true })
    );

    return this.refreshTokenRequest$;
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

  ensureAccessToken(): Observable<boolean> {
    const state = this.authState$.value;
    if (!state?.userKey) {
      return of(false);
    }

    if (state.accessToken) {
      return of(true);
    }

    return this.refreshToken().pipe(
      map(() => true),
      catchError(() => {
        this.clearLocalState();
        return of(false);
      })
    );
  }

  setKurinKey(kurinKey: string | null): void {
    const state = this.authState$.value;
    if (state) {
      const newState = { ...state, kurinKey };
      this.authState$.next(newState);
      this.persistAuthState(newState);
    }
  }

  updateEmail(email: string): void {
    const state = this.authState$.value;
    if (state) {
      const newState = { ...state, email };
      this.authState$.next(newState);
      this.persistAuthState(newState);
    }
  }

  updateRole(role: string): void {
    const state = this.authState$.value;
    if (state) {
      const newState = { ...state, role };
      this.authState$.next(newState);
      this.persistAuthState(newState);
    }
  }

  clearKurinKey(): void {
    this.setKurinKey(null);
  }

  private persistAuthState(state: AuthState): void {
    localStorage.setItem('authState', JSON.stringify(this.toStoredState(state)));
  }

  private deserializeStoredState(rawState: string): AuthState | null {
    try {
      const parsed = JSON.parse(rawState) as AuthState;
      return this.toStoredState(parsed);
    } catch {
      localStorage.removeItem('authState');
      return null;
    }
  }

  private toStoredState(state: AuthState): AuthState {
    return {
      userKey: state.userKey,
      memberKey: state.memberKey,
      email: state.email,
      role: state.role,
      kurinKey: state.kurinKey,
      accessToken: null
    };
  }
}
