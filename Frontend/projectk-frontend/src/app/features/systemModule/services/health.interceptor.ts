import { HttpErrorResponse, HttpEvent, HttpHandler, HttpInterceptor, HttpRequest } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, catchError, switchMap, throwError } from 'rxjs';
import { HealthBannerService } from './health-banner.service';

@Injectable()
export class HealthInterceptor implements HttpInterceptor {
  private readonly healthBannerService = inject(HealthBannerService);

  intercept(req: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    if (req.url.includes('/health')) {
      return next.handle(req);
    }

    return next.handle(req).pipe(
      catchError(error => {
        if (!(error instanceof HttpErrorResponse) || error.status !== 0) {
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
