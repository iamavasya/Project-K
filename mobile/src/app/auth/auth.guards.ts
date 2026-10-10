import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Capacitor } from '@capacitor/core';
import { AuthService } from './auth.service';

/**
 * Sign-in is for the PWA for now. The native shells cannot use the web's refresh cookie and wait
 * for the token-in-body refresh flow on the backend (PLAN.md §8), so they open without it.
 */
const signInRequired = !Capacitor.isNativePlatform();

/** Screens behind sign-in. A stored session is renewed from the cookie before the screen opens. */
export const signedInGuard: CanActivateFn = async () => {
  if (!signInRequired) return true;
  const router = inject(Router);
  return (await inject(AuthService).ensureSession()) || router.parseUrl('/login');
};

/** The sign-in screen is skipped when a session is already there. */
export const signedOutGuard: CanActivateFn = async () => {
  const router = inject(Router);
  return (await inject(AuthService).ensureSession()) ? router.parseUrl('/tabs/home') : true;
};
