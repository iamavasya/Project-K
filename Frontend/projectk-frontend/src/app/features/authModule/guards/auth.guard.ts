import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "../services/authService/auth.service";
import { inject } from "@angular/core";
import { map, of, switchMap, take } from "rxjs";

export const authGuard: CanActivateFn = () => {
    const authService = inject(AuthService);
    const router = inject(Router);

    return authService.getAuthState().pipe(
        take(1),
        switchMap(authState => {
            if (authState?.userKey) {
                return authService.ensureAccessToken();
            }
            return of(false);
        }),
        map(isAuthenticated => isAuthenticated ? true : router.createUrlTree(['/login']))
    );
}
