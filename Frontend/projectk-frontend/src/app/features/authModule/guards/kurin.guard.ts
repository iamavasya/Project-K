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
        // Planning is readable by anyone in the kurin — the backend settles it with the resource
        // check, and gating the route on leadership only hid a page the API was willing to serve.
        // Opening and deleting sessions stay gated, by their own controls.
        if (resource == 'planning-create' && !permissionService.canCreatePlanning()) {
            return router.createUrlTree(['/forbidden']);
        }
        return true;
    }
};
