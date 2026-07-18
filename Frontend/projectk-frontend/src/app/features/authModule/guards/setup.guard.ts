import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { catchError, map, of } from 'rxjs';
import { AuthService } from '../services/authService/auth.service';

export const setupGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.getSetupStatus().pipe(
    map(status => status.isInitialized ? router.createUrlTree(['/welcome']) : true),
    catchError(() => of(router.createUrlTree(['/welcome'])))
  );
};
