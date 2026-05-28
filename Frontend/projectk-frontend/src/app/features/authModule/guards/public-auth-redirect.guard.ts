import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, of, switchMap, take } from 'rxjs';
import { authenticatedHomeRoute } from '../functions/authenticated-home-route';
import { AuthService } from '../services/authService/auth.service';

export const publicAuthRedirectGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.getAuthState().pipe(
    take(1),
    switchMap(authState => {
      if (!authState?.userKey) {
        return of(true);
      }

      return authService.ensureAccessToken().pipe(
        map(isAuthenticated => isAuthenticated
          ? router.createUrlTree(authenticatedHomeRoute(authService.getAuthStateValue() ?? authState))
          : true)
      );
    })
  );
};
