import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/authService/auth.service';

export const leadershipAccessGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  const type = (route.paramMap.get('type') ?? '').toLowerCase();
  if (type !== 'kv') {
    return true;
  }

  const role = authService.getAuthStateValue()?.role;
  if (role === 'Manager' || role === 'Admin') {
    return true;
  }

  router.navigate(['/forbidden']);
  return false;
};
