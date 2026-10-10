import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, of, switchMap, throwError } from 'rxjs';
import { AuthService, isOffline } from './auth.service';

/** Sign-in steps answer 401 for a wrong password or code; that is not an expired session. */
const NO_REFRESH = ['/auth/login', '/auth/mfa/login-verify', '/auth/logout', '/auth/refresh'];

/**
 * Adds the access token (waiting for a renewal already under way when there is none yet) and, on a
 * 401, renews it from the refresh cookie once and retries.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const retryable = !NO_REFRESH.some((path) => req.url.includes(path));

  const token = auth.accessToken();
  const ready = retryable && !token ? from(auth.tokenWhenReady()) : of(token);

  return ready.pipe(
    switchMap((current) => next(withToken(req, current))),
    catchError((error: unknown) => {
      const expired = error instanceof HttpErrorResponse && error.status === 401;
      if (!retryable || !expired || !auth.signedIn()) {
        return throwError(() => error);
      }
      return from(auth.refresh()).pipe(
        catchError((refreshError: unknown) => {
          if (!isOffline(refreshError)) {
            auth.forget();
            void router.navigateByUrl('/login', { replaceUrl: true });
          }
          return throwError(() => error);
        }),
        switchMap((token) => next(withToken(req, token))),
      );
    }),
  );
};

function withToken<T>(req: HttpRequest<T>, token: string | null): HttpRequest<T> {
  return token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
}
