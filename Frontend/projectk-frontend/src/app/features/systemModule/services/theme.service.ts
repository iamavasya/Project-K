import { Injectable, computed, signal } from '@angular/core';

export type ThemePreference = 'light' | 'dark';

const STORAGE_KEY = 'lilyka-theme';

/**
 * Owns the `data-theme` attribute the PrimeNG preset switches on
 * (`darkModeSelector: '[data-theme="dark"]'`). The choice is per device, so it lives in
 * localStorage under its own key and deliberately survives sign-out.
 */
@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  private readonly preference = signal<ThemePreference>(this.readStored() ?? this.systemPreference());

  readonly current = this.preference.asReadonly();
  readonly isDark = computed(() => this.preference() === 'dark');

  constructor() {
    this.apply(this.preference());
  }

  toggle(): void {
    this.set(this.preference() === 'dark' ? 'light' : 'dark');
  }

  set(preference: ThemePreference): void {
    this.preference.set(preference);
    this.apply(preference);
    try {
      localStorage.setItem(STORAGE_KEY, preference);
    } catch {
      // A blocked storage quota must not break theming.
    }
  }

  private apply(preference: ThemePreference): void {
    const root = document.documentElement;
    if (preference === 'dark') {
      root.dataset['theme'] = 'dark';
    } else {
      delete root.dataset['theme'];
    }
  }

  private readStored(): ThemePreference | null {
    try {
      const stored = localStorage.getItem(STORAGE_KEY);
      return stored === 'dark' || stored === 'light' ? stored : null;
    } catch {
      return null;
    }
  }

  private systemPreference(): ThemePreference {
    return globalThis.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }
}
