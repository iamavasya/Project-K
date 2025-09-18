import { inject, Injectable } from "@angular/core";
import { AuthService } from "./auth.service";
import { HttpHandler, HttpInterceptor, HttpRequest } from "@angular/common/http";

@Injectable({
    providedIn: "root",
})

export class AuthInterceptor implements HttpInterceptor {
    private readonly authService = inject(AuthService);
    
    intercept(request: HttpRequest<any>, next: HttpHandler) {
        const token = this.authService.getAccessToken();
        if (token) {
            const cloned = request.clone({
                setHeaders: {
                    Authorization: `Bearer ${token}`
                }
            });
            return next.handle(cloned);
        }
        return next.handle(request);
    }
}