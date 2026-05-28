import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "../services/authService/auth.service";
import { PermissionService } from "../services/permission.service";
import { inject } from "@angular/core";

export const kurinAccessGuard = (resource: string): CanActivateFn => {
    return () => {
        const authService = inject(AuthService);
        const permissionService = inject(PermissionService);
        const router = inject(Router);

        const kurinKey = authService.getAuthStateValue()?.kurinKey;

        if (resource == 'kurin' && !kurinKey && permissionService.isAdmin()) {
            return router.createUrlTree(['/panel']);
        }
        if (resource == 'panel' && kurinKey) {
            return router.createUrlTree(['/kurin']);
        }
        if (resource == 'planning' && kurinKey && permissionService.getRole() === 'user') {
            return router.createUrlTree(['/kurin']);
        }
        if (resource == 'planning-create' && !permissionService.canManagePlanning()) {
            return router.createUrlTree(['/forbidden']);
        }
        return true;
    }
};
