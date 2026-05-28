import { AuthState } from '../models/auth-state.model';

export function authenticatedHomeRoute(state: AuthState | null | undefined): unknown[] {
  if (state?.kurinKey) {
    return ['/kurin'];
  }

  if (state?.role?.trim().toLowerCase() === 'admin') {
    return ['/panel'];
  }

  if (state?.memberKey) {
    return ['/member', state.memberKey];
  }

  return ['/login'];
}
