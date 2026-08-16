import { inject } from "@angular/core";
import { CanActivateFn, Router } from "@angular/router";
import { AuthService } from "../services/authService/auth.service";
import { PermissionService } from "../services/permission.service";
import { map, take } from "rxjs";

// The role names are capability hints: 'Admin' → admin, 'Manager' → whole-kurin manager,
// 'Mentor' → group leader. Backend permissions are the real gate; this only steers navigation.
export const roleGuard = (...requiredRoles: string[]): CanActivateFn => {
    return () => {
        const authService = inject(AuthService);
        const permissionService = inject(PermissionService);
        const router = inject(Router);

        return authService.getAuthState().pipe(
            take(1),
            map(() => {
                const wants = requiredRoles.map(role => role.trim().toLowerCase());
                const allowed =
                    (wants.includes('admin') && permissionService.isAdmin()) ||
                    (wants.includes('manager') && permissionService.isManager()) ||
                    (wants.includes('mentor') && permissionService.isMentor());

                return allowed ? true : router.createUrlTree(['/forbidden']);
            })
        );
    };
}
