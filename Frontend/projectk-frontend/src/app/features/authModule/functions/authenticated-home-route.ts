import { isUsableKey } from '../../../shared/functions/is-usable-key.function';
import { AuthState } from '../models/auth-state.model';

/**
 * Where a signed-in person lands: the dashboard when there is a person behind the account, the
 * kurin panel for an admin stepped into a kurin, the administration for one who is not.
 */
export function authenticatedHomeRoute(state: AuthState | null | undefined): unknown[] {
  if (isUsableKey(state?.memberKey)) {
    return ['/'];
  }

  if (isUsableKey(state?.kurinKey)) {
    return ['/kurin'];
  }

  if (state?.isAdmin) {
    return ['/panel'];
  }

  return ['/login'];
}
