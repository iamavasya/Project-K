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
            router.navigate(['/panel']);
            return false;
        }
        if (resource == 'panel' && kurinKey) {
            router.navigate(['/kurin']);
            return false;
        }
        if (resource == 'planning' && kurinKey && permissionService.getRole() === 'user') {
            router.navigate(['/kurin']);
            return false;
        }
        return true;
    }
};
