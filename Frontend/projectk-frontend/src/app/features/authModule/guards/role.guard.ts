import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "../services/auth.service";
import { map } from "rxjs";

export const roleGuard = (requiredRole: string): CanActivateFn => {
    return () => {
        const authService = inject(AuthService);
        const router = inject(Router);

        return authService.getAuthState().pipe(
            map(authState => {
                if (authState?.role === requiredRole) {
                    return true;
                } else {
                    router.navigate(['/forbidden']);
                    return false;
                }
            })
        );
    };
}