/* eslint-disable @typescript-eslint/no-explicit-any */
import { inject, Injectable } from "@angular/core";
import {
    HttpInterceptor,
    HttpRequest,
    HttpHandler,
    HttpEvent,
    HttpErrorResponse
} from "@angular/common/http";
import { AuthService } from "./authService/auth.service";
import { Observable, throwError, BehaviorSubject } from "rxjs";
import { catchError, switchMap, filter, take, finalize } from "rxjs/operators";
import { Router } from "@angular/router";

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
    private readonly authService = inject(AuthService);
    private readonly router = inject(Router);

    private isRefreshing = false;
    private readonly refreshTokenSubject = new BehaviorSubject<string | null>(null);

    intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        // Skip refresh token requests to avoid loops
        if (req.url.includes('/api/auth/refresh') || req.url.includes('/health')) {
            return next.handle(req);
        }

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
            if (req.url.includes('/api/auth/logout')) {
                return throwError(() => error);
            }
            return this.tryRefreshAndRepeat(req, next);
        }

        if (error instanceof HttpErrorResponse && error.status === 403) {
            this.router.navigate(['/forbidden']);
        }

        return throwError(() => error);
    }

    private tryRefreshAndRepeat(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
        if (this.isRefreshing) {
            return this.refreshTokenSubject.pipe(
                filter(token => token !== null),
                take(1),
                switchMap(token => {
                    if (token) {
                        return next.handle(this.addTokenToRequest(req, token));
                    }
                    return throwError(() => new Error('Session expired'));
                })
            );
        } else {
            this.isRefreshing = true;
            this.refreshTokenSubject.next(null);

            return this.authService.refreshToken().pipe(
                switchMap(newToken => {
                    this.isRefreshing = false;
                    this.refreshTokenSubject.next(newToken);
                    return next.handle(this.addTokenToRequest(req, newToken));
                }),
                catchError(() => {
                    this.isRefreshing = false;
                    this.refreshTokenSubject.next(null);
                    this.authService.clearLocalState();
                    this.router.navigate(['/login'], { replaceUrl: true });
                    return throwError(() => new Error('Session expired'));
                }),
                finalize(() => {
                    this.isRefreshing = false;
                })
            );
        }
    }

    private addTokenToRequest(req: HttpRequest<any>, token: string): HttpRequest<any> {
        return req.clone({
            setHeaders: { Authorization: `Bearer ${token}` }
        });
    }
}
