import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "../services/authService/auth.service";
import { map, take } from "rxjs";

export const roleGuard = (...requiredRoles: string[]): CanActivateFn => {
    return () => {
        const authService = inject(AuthService);
        const router = inject(Router);

        return authService.getAuthState().pipe(
            take(1),
            map(authState => {
                const role = authState?.role?.trim().toLowerCase();
                const allowedRoles = requiredRoles.map(requiredRole => requiredRole.trim().toLowerCase());

                if (role && allowedRoles.includes(role)) {
                    return true;
                } else {
                    return router.createUrlTree(['/forbidden']);
                }
            })
        );
    };
}
