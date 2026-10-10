import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { isOffline } from '../auth/auth.service';
import { apiUrl } from '../runtime-config';

type Query = Record<string, string | number | boolean | null | undefined | readonly (string | number)[]>;

/**
 * The one way features talk to the API: paths are relative to `…/api`, the auth interceptor adds
 * the token, and every call is a promise (signals hold the result, no streams to unsubscribe).
 */
@Injectable({ providedIn: 'root' })
export class Api {
  private readonly http = inject(HttpClient);
  private readonly base = apiUrl();

  get<T>(path: string, query?: Query): Promise<T> {
    return firstValueFrom(this.http.get<T>(this.url(path), { params: params(query) }));
  }

  post<T>(path: string, body: unknown = {}, query?: Query): Promise<T> {
    return firstValueFrom(this.http.post<T>(this.url(path), body, { params: params(query) }));
  }

  put<T>(path: string, body: unknown = {}): Promise<T> {
    return firstValueFrom(this.http.put<T>(this.url(path), body));
  }

  patch<T>(path: string, body: unknown = {}): Promise<T> {
    return firstValueFrom(this.http.patch<T>(this.url(path), body));
  }

  delete<T>(path: string, query?: Query): Promise<T> {
    return firstValueFrom(this.http.delete<T>(this.url(path), { params: params(query) }));
  }

  /** A binary download (a PDF report, an image) as a Blob. */
  blob(path: string, query?: Query): Promise<Blob> {
    return firstValueFrom(this.http.get(this.url(path), { params: params(query), responseType: 'blob' }));
  }

  private url(path: string): string {
    return `${this.base}/${path.replace(/^\//, '')}`;
  }
}

function params(query: Query | undefined): HttpParams | undefined {
  if (!query) return undefined;
  let result = new HttpParams();
  for (const [key, value] of Object.entries(query)) {
    if (value === null || value === undefined || value === '') continue;
    if (Array.isArray(value)) {
      for (const item of value) result = result.append(key, String(item));
    } else {
      result = result.set(key, String(value));
    }
  }
  return result;
}

/** Shared wording for the API's error codes (`{ error, message }`); a feature adds its own. */
const COMMON_ERRORS: Record<string, string> = {
  Forbidden: 'Недостатньо прав для цієї дії.',
  NotFound: 'Цього вже немає.',
  ValidationFailed: 'Перевір поля форми.',
  Conflict: 'Хтось уже змінив це. Онови і спробуй ще раз.',
};

/**
 * What to tell the person when a call failed. Never the API's own message (English, technical):
 * a known code, else wording by status, else the screen's fallback.
 */
export function apiErrorText(error: unknown, fallback: string, known: Record<string, string> = {}): string {
  if (!(error instanceof HttpErrorResponse)) return fallback;
  if (isOffline(error)) return 'Немає зв’язку з сервером. Перевір інтернет.';
  const code = (error.error as { error?: string } | null)?.error;
  if (code && known[code]) return known[code];
  if (code && COMMON_ERRORS[code]) return COMMON_ERRORS[code];
  if (error.status === 403) return COMMON_ERRORS['Forbidden'];
  if (error.status === 404) return COMMON_ERRORS['NotFound'];
  if (error.status === 409) return COMMON_ERRORS['Conflict'];
  if (error.status === 429) return 'Забагато спроб. Спробуй за хвилину.';
  return fallback;
}
