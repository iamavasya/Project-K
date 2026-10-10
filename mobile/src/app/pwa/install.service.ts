import { Injectable, computed, signal } from '@angular/core';
import { isPlatform } from '@ionic/angular';

/** Chrome's install prompt event (not in lib.dom yet). */
interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

/**
 * Add-to-home-screen for the PWA. Android/Chromium fires beforeinstallprompt and we show our own
 * button; iOS Safari has no prompt API, so we show how to do it from the Share sheet instead.
 */
@Injectable({ providedIn: 'root' })
export class InstallService {
  private readonly deferred = signal<BeforeInstallPromptEvent | null>(null);
  private readonly installed = signal(isStandalone());

  /** Running from the home screen (or inside the native shell): nothing to offer. */
  readonly standalone = this.installed.asReadonly();
  /** Chromium can show its install dialog right now. */
  readonly canPrompt = computed(() => !this.installed() && this.deferred() !== null);
  /** iPhone/iPad Safari tab: installing is a manual Share → Add to Home Screen. */
  readonly needsIosHint = computed(
    () => !this.installed() && isPlatform('ios') && !isPlatform('capacitor'),
  );

  constructor() {
    if (isPlatform('capacitor')) {
      this.installed.set(true);
      return;
    }
    globalThis.addEventListener('beforeinstallprompt', (event) => {
      event.preventDefault();
      this.deferred.set(event as BeforeInstallPromptEvent);
    });
    globalThis.addEventListener('appinstalled', () => {
      this.deferred.set(null);
      this.installed.set(true);
    });
  }

  async prompt(): Promise<boolean> {
    const event = this.deferred();
    if (!event) {
      return false;
    }
    this.deferred.set(null);
    await event.prompt();
    const { outcome } = await event.userChoice;
    return outcome === 'accepted';
  }
}

function isStandalone(): boolean {
  const iosStandalone = (navigator as Navigator & { standalone?: boolean }).standalone === true;
  return iosStandalone || globalThis.matchMedia?.('(display-mode: standalone)').matches === true;
}
