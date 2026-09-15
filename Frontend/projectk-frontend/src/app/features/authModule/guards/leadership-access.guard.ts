import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { LeadershipScope, PermissionService } from '../services/permission-service/permission.service';

const scopes: readonly LeadershipScope[] = ['kv', 'kurin', 'group'];

/** The провід form: the `:type` in the address decides whose body it is, and the gate follows it. */
export const leadershipAccessGuard: CanActivateFn = (route) => {
  const permissionService = inject(PermissionService);
  const router = inject(Router);

  const type = route.paramMap.get('type') as LeadershipScope | null;
  if (type && scopes.includes(type) && permissionService.canSetupLeadership(type)) {
    return true;
  }

  router.navigate(['/forbidden']);
  return false;
};
