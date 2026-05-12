import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { PermissionService } from '../services/permission.service';

export const leadershipAccessGuard: CanActivateFn = (route) => {
  const permissionService = inject(PermissionService);
  const router = inject(Router);

  const type = (route.paramMap.get('type') ?? '').toLowerCase();
  if (type !== 'kv') {
    return true;
  }

  if (permissionService.canManageGroups()) {
    return true;
  }

  router.navigate(['/forbidden']);
  return false;
};
