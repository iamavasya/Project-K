import { inject, Injectable } from "@angular/core";
import { AuthState } from "../models/auth-state.model";
import { BehaviorSubject, map, Observable, tap } from "rxjs";
import { HttpClient } from "@angular/common/http";
import { environment } from "../../environments/environment";
import { LoginRequest } from "../models/login-request.model";
import { LoginResponse, UserSession } from "../models/login-response.model";

@Injectable({
    providedIn: 'root'
})

export class AuthService {
    private readonly apiUrl = environment.apiUrl;
    private authState$ = new BehaviorSubject<AuthState | null>(null);
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

    login(credentials: LoginRequest): Observable<UserSession> {
        return this.http.post<LoginResponse>(`${this.apiUrl}/auth/login`, credentials)
        .pipe(
            map(response => ({
                userKey: response.userKey,
                email: response.email,
                role: response.role,
                accessToken: response.tokens.accessToken,
                refreshToken: response.tokens.refreshToken.token,
                expires: new Date(response.tokens.refreshToken.expires)
            })),
            tap(session => {
                this.authState$.next(session);
                localStorage.setItem('authState', JSON.stringify(session));
            })
        );
    }

    logout() {
        return this.http.post(`${this.apiUrl}/auth/logout`, {}).pipe(
                tap({
                    next: () => {
                        this.authState$.next(null);
                        localStorage.removeItem('authState');
                    },
                    error: () => {
                        this.authState$.next(null);
                        localStorage.removeItem('authState');
                    }
            })
        );
    }
    
    getAccessToken(): string | null {
        return this.authState$.value?.accessToken ?? null;
    }
}