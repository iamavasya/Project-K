import { Injectable, computed, signal } from '@angular/core';
import {
  INSTALL_BANNER_DISMISSED_KEY,
  isMobileUserAgent,
  PHONE_MEDIA_QUERY,
  shouldShowInstallBanner
} from '../../functions/install-app.function';
import { PREFER_WEB_KEY } from '../../functions/mobile-redirect.function';

/** Chrome's install prompt event (not in lib.dom). */
interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

/**
 * Chrome can fire beforeinstallprompt before Angular is up, so main.ts starts listening first and the
 * event waits here for the service.
 */
let early: BeforeInstallPromptEvent | null = null;

export function captureInstallPrompt(): void {
  globalThis.addEventListener?.('beforeinstallprompt', event => {
    // No mini-infobar of Chrome's own: the banner offers the install.
    event.preventDefault();
    early = event as BeforeInstallPromptEvent;
  });
  globalThis.addEventListener?.('appinstalled', () => {
    early = null;
  });
}

/**
 * The phone app is the PWA under /m/. The web links its manifest (public/manifest.webmanifest: the
 * PWA's id and icons, start_url /m/), so Chrome on Android offers to install it right from a web
 * page and «Встановити» is one tap; elsewhere (iPhone, Firefox, an app's own browser) the button
 * opens /m/install, which shows the way by hand with pictures. Phones are mostly sent to /m/
 * before the web starts (mobile-redirect.function.ts), so on a phone the banner is a fallback and
 * the usual sight is «Мобільна версія» for those who chose «Повна версія».
 */
@Injectable({ providedIn: 'root' })
export class InstallAppService {
  private readonly deferred = signal<BeforeInstallPromptEvent | null>(early);
  private readonly dismissedAt = signal(readDismissedAt());
  private readonly installed = signal(false);
  private readonly preferWeb = signal(readPreferWeb());
  private readonly phone =
    globalThis.matchMedia?.(PHONE_MEDIA_QUERY).matches === true || isMobileUserAgent(globalThis.navigator?.userAgent ?? '');

  /**
   * A phone on the web is one that chose «Повна версія» (main.ts sends the others to /m/): it gets
   * a quiet way back instead of the install offer.
   */
  readonly offersMobileVersion = computed(() => this.phone && this.preferWeb());

  /** Chrome's install dialog can open right now. */
  readonly canPrompt = computed(() => this.deferred() !== null && !this.installed());

  readonly shouldOffer = computed(() =>
    !this.installed() &&
    !this.preferWeb() &&
    shouldShowInstallBanner({
      phoneScreen: globalThis.matchMedia?.(PHONE_MEDIA_QUERY).matches === true,
      userAgent: globalThis.navigator?.userAgent ?? '',
      standalone: isStandalone(),
      dismissedAt: this.dismissedAt(),
      now: Date.now()
    })
  );

  constructor() {
    globalThis.addEventListener?.('beforeinstallprompt', event => {
      event.preventDefault();
      this.deferred.set(event as BeforeInstallPromptEvent);
    });
    globalThis.addEventListener?.('appinstalled', () => {
      this.deferred.set(null);
      this.installed.set(true);
    });
  }

  /** Chrome's own dialog, from the person's tap. True when they installed. */
  async prompt(): Promise<boolean> {
    const event = this.deferred();
    if (!event) {
      return false;
    }
    // One event, one dialog; Chrome fires a new one later if the answer was no.
    this.deferred.set(null);
    early = null;
    await event.prompt();
    const { outcome } = await event.userChoice;
    if (outcome === 'accepted') {
      this.installed.set(true);
    }
    return outcome === 'accepted';
  }

  /** «Мобільна версія»: the phone goes to /m/ again from now on. */
  forgetPreferWeb(): void {
    this.preferWeb.set(false);
    try {
      localStorage.removeItem(PREFER_WEB_KEY);
    } catch {
      // Nothing was stored, then.
    }
  }

  /** «Не зараз»: remembered on this device; without storage the banner just comes back next visit. */
  dismiss(): void {
    const now = Date.now();
    this.dismissedAt.set(now);
    try {
      localStorage.setItem(INSTALL_BANNER_DISMISSED_KEY, String(now));
    } catch {
      // Private mode or blocked storage: dismissed for this visit only.
    }
  }
}

function readPreferWeb(): boolean {
  try {
    return localStorage.getItem(PREFER_WEB_KEY) === '1';
  } catch {
    return false;
  }
}

function readDismissedAt(): number | null {
  try {
    const at = Number(localStorage.getItem(INSTALL_BANNER_DISMISSED_KEY));
    return Number.isFinite(at) && at > 0 ? at : null;
  } catch {
    return null;
  }
}

function isStandalone(): boolean {
  const iosStandalone = (globalThis.navigator as Navigator & { standalone?: boolean } | undefined)?.standalone === true;
  return iosStandalone || globalThis.matchMedia?.('(display-mode: standalone)').matches === true;
}
