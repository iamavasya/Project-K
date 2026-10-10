import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { Capacitor } from '@capacitor/core';
import { AuthService } from './auth.service';

/**
 * Sign-in is for the PWA for now. The native shells cannot use the web's refresh cookie and wait
 * for the token-in-body refresh flow on the backend (PLAN.md §8), so they open without it.
 */
const signInRequired = !Capacitor.isNativePlatform();

/** Screens behind sign-in. */
export const signedInGuard: CanActivateFn = () => {
  if (!signInRequired) return true;
  return inject(AuthService).ensureSession() || inject(Router).parseUrl('/login');
};

/** The sign-in screen is skipped when a session is already there. */
export const signedOutGuard: CanActivateFn = () =>
  inject(AuthService).ensureSession() ? inject(Router).parseUrl('/tabs/home') : true;
