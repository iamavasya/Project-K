import { isUsableKey } from '../../../shared/functions/isUsableKey.function';
import { AuthState } from '../models/auth-state.model';

export function authenticatedHomeRoute(state: AuthState | null | undefined): unknown[] {
  if (isUsableKey(state?.kurinKey)) {
    return ['/kurin'];
  }

  if (state?.isAdmin) {
    return ['/panel'];
  }

  if (isUsableKey(state?.memberKey)) {
    return ['/member', state?.memberKey];
  }

  return ['/login'];
}
