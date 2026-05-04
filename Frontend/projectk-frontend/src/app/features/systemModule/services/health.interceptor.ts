import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, TimeoutError, catchError, switchMap, throwError, timeout } from 'rxjs';
import { HealthBannerService } from './health-banner.service';

@Injectable()
export class HealthInterceptor implements HttpInterceptor {
  private readonly healthBannerService = inject(HealthBannerService);

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (req.url.includes('/health')) {
      return next.handle(req);
    }

    const shouldTimeout = this.healthBannerService.shouldApplyTimeoutRequests();
    const request$ = shouldTimeout
      ? next.handle(req).pipe(timeout({ first: 10000 }))
      : next.handle(req);

    return request$.pipe(
      catchError(error => {
        const isTimeout = error instanceof TimeoutError;
        const isNetworkError = error instanceof HttpErrorResponse && error.status === 0;

        if (!isTimeout && !isNetworkError) {
          return throwError(() => error);
        }

        if (req.headers.has('x-health-retry')) {
          return throwError(() => error);
        }

        return this.healthBannerService.waitForHealthy().pipe(
          switchMap(() => {
            const retryRequest = req.clone({
              headers: req.headers.set('x-health-retry', '1')
            });
            return next.handle(retryRequest);
          })
        );
      })
    );
  }
}
