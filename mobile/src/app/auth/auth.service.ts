import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { apiUrl } from '../runtime-config';
import { AuthState, LoginOutcome, LoginResponse } from './auth.models';

/**
 * The PWA lives on the web's origin (/m/), so it shares the web's session: the same `authState`
 * entry in localStorage and the same httpOnly refresh cookie on the API. Signing in or out in one
 * signs in or out of the other, which is the only consistent option while the cookie is shared.
 * Like the web, the access token stays in memory only and is renewed from the cookie after a reload.
 */
const STORAGE_KEY = 'authState';

const ERROR_TEXT: Record<string, string> = {
  InvalidCredentials: 'Невірний email або пароль.',
  Unauthorized: 'Невірний email або пароль.',
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly api = apiUrl();
  private readonly state = signal<AuthState | null>(readStoredState());
  private refreshing: Promise<string> | null = null;

  readonly user = this.state.asReadonly();
  readonly signedIn = computed(() => this.state() !== null);

  accessToken(): string | null {
    return this.state()?.accessToken ?? null;
  }

  async login(email: string, password: string): Promise<LoginOutcome> {
    const response = await this.post<LoginResponse>('auth/login', { email, password });
    if (response.requiresMfa || !response.tokens) {
      return { kind: 'mfa', mfaToken: response.mfaToken ?? null };
    }
    this.apply(toState(response));
    return { kind: 'signed-in' };
  }

  /** The second step; `code` is a 6-digit authenticator code or a recovery code. */
  async verifyMfa(email: string, code: string, mfaToken: string | null): Promise<void> {
    const response = await this.post<LoginResponse>('auth/mfa/login-verify', {
      email,
      code,
      rememberMe: true,
      mfaToken,
    });
    this.apply(toState(response));
  }

  /** One request at a time: every caller waiting on a 401 shares it. */
  refresh(): Promise<string> {
    this.refreshing ??= this.post<{ accessToken: string }>('auth/refresh', {})
      .then(({ accessToken }) => {
        const current = this.state();
        if (current) this.apply({ ...current, accessToken });
        return accessToken;
      })
      .finally(() => (this.refreshing = null));
    return this.refreshing;
  }

  /** True when there is a usable session, renewing the access token from the cookie if needed. */
  async ensureSession(): Promise<boolean> {
    if (!this.state()) return false;
    if (this.accessToken()) return true;
    try {
      await this.refresh();
      return true;
    } catch (error) {
      // Offline is not signed out: keep the session and let the cached screens open.
      if (error instanceof HttpErrorResponse && error.status === 0) return true;
      this.forget();
      return false;
    }
  }

  /** Ends the session on the server first, so the request still carries the token. */
  async logout(): Promise<void> {
    try {
      if (await this.ensureSession()) await this.post('auth/logout', {}, 'text');
    } catch {
      // Signed out locally either way; the server session expires on its own.
    } finally {
      this.forget();
    }
  }

  forget(): void {
    this.state.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  private apply(state: AuthState): void {
    this.state.set(state);
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ ...state, accessToken: null }));
  }

  private post<T>(path: string, body: unknown, responseType: 'json' | 'text' = 'json'): Promise<T> {
    return firstValueFrom(
      this.http.post<T>(`${this.api}/${path}`, body, {
        withCredentials: true,
        responseType: responseType as 'json',
      }),
    );
  }
}

/** What to tell a person whose sign-in failed. The API's own messages are English and technical. */
export function loginErrorText(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) return 'Немає зв’язку з сервером. Перевір інтернет.';
    if (error.status === 429) return 'Забагато спроб. Спробуй за хвилину.';
    const code = (error.error as { error?: string } | null)?.error;
    if (code && ERROR_TEXT[code]) return ERROR_TEXT[code];
  }
  return fallback;
}

function toState(response: LoginResponse): AuthState {
  if (!response.tokens) throw new Error('No tokens in the sign-in response');
  return {
    userKey: response.userKey,
    memberKey: response.memberKey,
    email: response.email,
    isAdmin: response.isAdmin,
    permissions: response.permissions ?? [],
    roles: response.roles ?? [],
    kurinKey: response.kurinKey ?? null,
    accessToken: response.tokens.accessToken,
  };
}

function readStoredState(): AuthState | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as AuthState;
    return parsed?.userKey ? { ...parsed, accessToken: null } : null;
  } catch {
    localStorage.removeItem(STORAGE_KEY);
    return null;
  }
}
