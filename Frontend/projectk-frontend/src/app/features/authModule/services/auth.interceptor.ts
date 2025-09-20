import { inject, Injectable } from "@angular/core";
import {
    HttpInterceptor,
    HttpRequest,
    HttpHandler,
    HttpEvent,
    HttpErrorResponse
} from "@angular/common/http";
import { AuthService } from "./auth.service";
import { Observable, throwError, BehaviorSubject } from "rxjs";
import { catchError, switchMap, filter, take } from "rxjs/operators";

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    private readonly authService = inject(AuthService);

    private isRefreshing = false;
    private refreshTokenSubject = new BehaviorSubject<string | null>(null);

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        const authReq = this.addAuthHeader(req);

        return next.handle(authReq).pipe(
            catchError(error => this.handleError(error, req, next))
        );
    }

    private addAuthHeader(req: HttpRequest<any>): HttpRequest<any> {
        const token = this.authService.getAccessToken();
        if (!token) return req;

        return req.clone({
            setHeaders: { Authorization: `Bearer ${token}` }
        });
    }

    private handleError(error: any, req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        if (error instanceof HttpErrorResponse && error.status === 401) {
            return this.tryRefreshAndRepeat(req, next);
        }
        return throwError(() => error);
    }

    private tryRefreshAndRepeat(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        if (!this.isRefreshing) {
            this.isRefreshing = true;
            this.refreshTokenSubject.next(null);

            return this.authService.refreshToken().pipe(
                switchMap(newToken => {
                    this.isRefreshing = false;
                    this.refreshTokenSubject.next(newToken);
                    const retryReq = req.clone({
                        setHeaders: { Authorization: `Bearer ${newToken}` }
                    });
                    return next.handle(retryReq);
                }),
                catchError(err => {
                    this.isRefreshing = false;
                    return throwError(() => err);
                })
            );
        } else {
            // Wait until the refresh is done, then retry
            return this.refreshTokenSubject.pipe(
                filter(token => !!token),
                take(1),
                switchMap(token => {
                    const retryReq = req.clone({
                        setHeaders: { Authorization: `Bearer ${token}` }
                    });
                    return next.handle(retryReq);
                })
            );
        }
    }
}
