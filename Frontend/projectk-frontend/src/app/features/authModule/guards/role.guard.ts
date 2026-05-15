import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "../services/authService/auth.service";
import { map, take } from "rxjs";

export const roleGuard = (requiredRole: string): CanActivateFn => {
    return () => {
        const authService = inject(AuthService);
        const router = inject(Router);

        return authService.getAuthState().pipe(
            take(1),
            map(authState => {
                if (authState?.role?.toLowerCase() === requiredRole.toLowerCase()) {
                    return true;
                } else {
                    return router.createUrlTree(['/forbidden']);
                }
            })
        );
    };
}
