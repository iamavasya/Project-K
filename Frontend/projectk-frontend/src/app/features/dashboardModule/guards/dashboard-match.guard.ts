import { inject } from '@angular/core';
import { CanMatchFn } from '@angular/router';
import { map, of, switchMap, take } from 'rxjs';
import { isUsableKey } from '../../../shared/functions/is-usable-key.function';
import { AuthService } from '../../authModule/services/auth-service/auth.service';

/**
 * Lets `/` be the dashboard for a signed-in person with a card, and nothing for anyone else — so
 * the welcome page behind it takes over for a guest or an admin with no card of their own.
 */
export const dashboardMatchGuard: CanMatchFn = () => {
  const authService = inject(AuthService);

  return authService.getAuthState().pipe(
    take(1),
    switchMap(state => {
      if (!state?.userKey || !isUsableKey(state.memberKey)) {
        return of(false);
      }
      return authService.ensureAccessToken().pipe(map(isAuthenticated => isAuthenticated));
    })
  );
};
