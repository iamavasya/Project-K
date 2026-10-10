import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/** Sign-in steps answer 401 for a wrong password or code; that is not an expired session. */
const NO_REFRESH = ['/auth/login', '/auth/mfa/login-verify', '/auth/logout', '/auth/refresh'];

/** Adds the access token and, on a 401, renews it from the refresh cookie once and retries. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  if (NO_REFRESH.some((path) => req.url.includes(path))) return next(req);

  return next(withToken(req, auth.accessToken())).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || !auth.signedIn()) {
        return throwError(() => error);
      }
      return from(auth.refresh()).pipe(
        catchError(() => {
          auth.forget();
          void router.navigateByUrl('/login', { replaceUrl: true });
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
