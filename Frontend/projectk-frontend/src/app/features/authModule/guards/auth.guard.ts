import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "../services/auth.service";
import { inject } from "@angular/core";
import { map } from "rxjs";

export const authGuard: CanActivateFn = () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    return authService.getAuthState().pipe(
        map(authState => {
            if (authState?.accessToken) {
                return true;
            } else {
                router.navigate(['/login']);
                return false;
            }
        })
    );
}