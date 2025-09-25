import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "../services/auth.service";
import { inject } from "@angular/core";

export const kurinGuard = (route: string): CanActivateFn => {
    return () => {
        const authService = inject(AuthService);
        const router = inject(Router);

        if (route == 'kurin' && !authService.getAuthStateValue()?.kurinKey && authService.getAuthStateValue()?.role === 'Admin') {
            router.navigate(['/panel']);
            return false;
        }
        if (route == 'panel' && authService.getAuthStateValue()?.kurinKey) {
            router.navigate(['/kurin', authService.getAuthStateValue()?.kurinKey]);
            return false;
        }
        return true;
    }
};
