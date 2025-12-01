import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "../services/authService/auth.service";
import { inject } from "@angular/core";

export const kurinAccessGuard = (resource: string): CanActivateFn => {
    return () => {
        const authService = inject(AuthService);
        const router = inject(Router);

        if (resource == 'kurin' && !authService.getAuthStateValue()?.kurinKey && authService.getAuthStateValue()?.role === 'Admin') {
            router.navigate(['/panel']);
            return false;
        }
        if (resource == 'panel' && authService.getAuthStateValue()?.kurinKey) {
            router.navigate(['/kurin', authService.getAuthStateValue()?.kurinKey]);
            return false;
        }
        if (resource == 'planning' && authService.getAuthStateValue()?.kurinKey && authService.getAuthStateValue()?.role === 'User') {
            router.navigate(['/kurin', authService.getAuthStateValue()?.kurinKey]);
            return false;
        }
        return true;
    }
};
