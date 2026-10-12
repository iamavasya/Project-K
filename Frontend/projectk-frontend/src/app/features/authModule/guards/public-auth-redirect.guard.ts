import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map, of, switchMap, take } from 'rxjs';
import { authenticatedHomeRoute } from '../functions/authenticated-home-route';
import { AuthService } from '../services/auth-service/auth.service';

export const publicAuthRedirectGuard: CanActivateFn = (route) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  // «Додати акаунт» signs in a second account while the first stays signed in.
  const addingAccount = route.queryParamMap.get('add') === '1';

  return authService.getAuthState().pipe(
    take(1),
    switchMap(authState => {
      if (!authState?.userKey || addingAccount) {
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
