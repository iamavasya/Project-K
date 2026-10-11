import { Injectable, signal } from '@angular/core';

/**
 * How the app is moved around (Вигляд → Навігація):
 * - `tabs`: four tabs and «Меню», which repeats the web's sidebar;
 * - `web`: no tabs, the web's ☰ and its sidebar, as the web is on a phone.
 */
export type NavMode = 'tabs' | 'web';

const STORAGE_KEY = 'lileyka-mobile-nav';

/** Per device, like the theme; it survives sign-out. */
@Injectable({ providedIn: 'root' })
export class NavModeService {
  private readonly stored = signal<NavMode>(readStored());
  readonly mode = this.stored.asReadonly();

  set(mode: NavMode): void {
    this.stored.set(mode);
    try {
      if (mode === 'tabs') localStorage.removeItem(STORAGE_KEY);
      else localStorage.setItem(STORAGE_KEY, mode);
    } catch {
      // Storage refused: the choice holds until the app closes.
    }
  }
}

function readStored(): NavMode {
  try {
    return localStorage.getItem(STORAGE_KEY) === 'web' ? 'web' : 'tabs';
  } catch {
    return 'tabs';
  }
}
