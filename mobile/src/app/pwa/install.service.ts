import { Injectable, computed, signal } from '@angular/core';
import { isPlatform } from '@ionic/angular';

/** Chrome's install prompt event (not in lib.dom yet). */
export interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

/**
 * Chrome may fire beforeinstallprompt before Angular has started (the runtime config is fetched
 * first) or before anything has asked for InstallService, so main.ts listens from the very start
 * and keeps the event here until the service picks it up.
 */
let early: BeforeInstallPromptEvent | null = null;
let installedEarly = false;

export function captureInstallPrompt(): void {
  globalThis.addEventListener?.('beforeinstallprompt', (event) => {
    // Keeps Chrome's own mini-infobar away: the app offers the button itself.
    event.preventDefault();
    early = event as BeforeInstallPromptEvent;
  });
  globalThis.addEventListener?.('appinstalled', () => {
    early = null;
    installedEarly = true;
  });
}

/**
 * Add-to-home-screen for the PWA. Android/Chromium fires beforeinstallprompt and we show our own
 * button; iOS Safari has no prompt API, so /install shows how to do it from the Share sheet instead.
 */
@Injectable({ providedIn: 'root' })
export class InstallService {
  private readonly deferred = signal<BeforeInstallPromptEvent | null>(early);
  private readonly running = signal(isStandalone());
  private readonly added = signal(installedEarly);

  /** Running from the home screen (or inside the native shell): nothing to offer. */
  readonly standalone = this.running.asReadonly();
  /** Installed from this tab just now (the system dialog said yes, or appinstalled came). */
  readonly justInstalled = this.added.asReadonly();
  /** Chromium can show its install dialog right now. */
  readonly canPrompt = computed(() => !this.running() && !this.added() && this.deferred() !== null);
  /** iPhone/iPad Safari tab: installing is a manual Share → Add to Home Screen. */
  readonly needsIosHint = computed(
    () => !this.running() && isPlatform('ios') && !isPlatform('capacitor'),
  );

  constructor() {
    if (isPlatform('capacitor')) {
      this.running.set(true);
      return;
    }
    globalThis.addEventListener('beforeinstallprompt', (event) => {
      event.preventDefault();
      this.deferred.set(event as BeforeInstallPromptEvent);
    });
    globalThis.addEventListener('appinstalled', () => {
      this.deferred.set(null);
      this.added.set(true);
    });
  }

  /** Chrome's own install dialog, from the person's tap. True when they said «Встановити». */
  async prompt(): Promise<boolean> {
    const event = this.deferred();
    if (!event) {
      return false;
    }
    // The event works once; Chrome fires a fresh one later if the person said no.
    this.deferred.set(null);
    early = null;
    await event.prompt();
    const { outcome } = await event.userChoice;
    if (outcome === 'accepted') {
      this.added.set(true);
    }
    return outcome === 'accepted';
  }
}

function isStandalone(): boolean {
  const iosStandalone = (navigator as Navigator & { standalone?: boolean }).standalone === true;
  return iosStandalone || globalThis.matchMedia?.('(display-mode: standalone)').matches === true;
}
