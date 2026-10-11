import { Injectable, computed, inject, signal } from '@angular/core';
import { SwPush } from '@angular/service-worker';
import { isPlatform } from '@ionic/angular';
import { firstValueFrom, race, timer, map } from 'rxjs';
import { Api } from '../core/api';
import { InstallService } from './install.service';
import { PushState, pushState, sameKey } from './push';

interface PublicKey {
  publicKey: string | null;
}

const SUBSCRIPTIONS = 'notifications/push/subscriptions';

/**
 * Web Push for the PWA: what lands in the bell also reaches the phone while the app is closed. The
 * Angular service worker shows the push and opens the screen it names on a tap (the server sends
 * ngsw's notification format), so this side only manages the subscription: asks the permission,
 * subscribes with the server's VAPID key, and tells the API which device to send to. iPhone gets
 * pushes only in the home-screen app (iOS 16.4+), Android Chrome always.
 */
@Injectable({ providedIn: 'root' })
export class PushService {
  private readonly sw = inject(SwPush);
  private readonly api = inject(Api);
  private readonly install = inject(InstallService);

  private readonly serverKey = signal<string | null | undefined>(undefined);
  private readonly permission = signal<NotificationPermission | null>(currentPermission());
  private readonly subscribed = signal<boolean | null>(null);
  private loading: Promise<void> | null = null;

  readonly busy = signal(false);
  /** The server sends pushes: the row is worth showing (its state may still be loading). */
  readonly available = computed(() => typeof this.serverKey() === 'string');
  readonly state = computed<PushState>(() =>
    pushState({
      serverKey: this.serverKey(),
      iphone: isPlatform('ios'),
      standalone: this.install.standalone(),
      supported: this.supported(),
      permission: this.permission(),
      subscribed: this.subscribed(),
    }),
  );

  /**
   * Reads the server's key and this device's state, once per app run. A device that already gets
   * pushes sends its subscription again: browsers rotate endpoints now and then, and the server
   * only knows the one it was last told.
   */
  load(): Promise<void> {
    this.loading ??= this.read();
    return this.loading;
  }

  /** Asks the permission (from the tap itself, as iOS requires), subscribes and registers the device. */
  async enable(): Promise<void> {
    const key = this.serverKey();
    if (!key || !this.supported()) return;
    this.busy.set(true);
    try {
      const permission = await Notification.requestPermission();
      this.permission.set(permission);
      if (permission !== 'granted') return;
      const subscription = await this.sw.requestSubscription({ serverPublicKey: key });
      try {
        await this.api.post(SUBSCRIPTIONS, subscription.toJSON());
      } catch (error) {
        // The server will not send to it, so the browser should not keep it either.
        await this.sw.unsubscribe().catch(() => undefined);
        throw error;
      }
      this.subscribed.set(true);
    } finally {
      this.busy.set(false);
    }
  }

  /** Stops pushes to this device on both sides. The browser side goes even if the server is unreachable. */
  async disable(): Promise<void> {
    this.busy.set(true);
    try {
      await this.drop();
    } finally {
      this.busy.set(false);
    }
  }

  /**
   * Signing out on this device: the next person to sign in here must not get the previous one's
   * pushes, so the subscription goes on both sides. Never holds up the sign-out.
   */
  async forgetDevice(): Promise<void> {
    // Nothing to forget where the server sends no pushes or the device is known to have none.
    if (this.supported() && this.serverKey() !== null && this.subscribed() !== false) {
      await this.drop().catch(() => undefined);
    }
    this.loading = null;
    this.serverKey.set(undefined);
    this.subscribed.set(null);
  }

  private async read(): Promise<void> {
    const key = await this.api
      .get<PublicKey>('notifications/push/public-key')
      .then((answer) => answer.publicKey ?? null)
      .catch(() => null);
    this.serverKey.set(key);
    this.permission.set(currentPermission());
    if (!key || !this.supported() || this.state() === 'install') return;

    // On a first visit this waits until the service worker is in charge; the row says «loading».
    const subscription = await firstValueFrom(this.sw.subscription).catch(() => null);
    if (!subscription) {
      this.subscribed.set(false);
      return;
    }
    if (this.permission() !== 'granted') {
      this.subscribed.set(true);
      return;
    }
    try {
      if (sameKey(subscription.options?.applicationServerKey, key)) {
        await this.api.post(SUBSCRIPTIONS, subscription.toJSON());
      } else {
        await this.sw.unsubscribe();
        const fresh = await this.sw.requestSubscription({ serverPublicKey: key });
        await this.api.post(SUBSCRIPTIONS, fresh.toJSON());
      }
      this.subscribed.set(true);
    } catch {
      // Offline or refused: the device keeps what it has and tries again on the next start.
      this.subscribed.set(true);
    }
  }

  private async drop(): Promise<void> {
    // A service worker that is not in charge yet has no subscription to give; don't wait for it.
    const subscription = await firstValueFrom(
      race(this.sw.subscription, timer(1500).pipe(map(() => null))),
    ).catch(() => null);
    if (subscription) {
      await this.api.delete(SUBSCRIPTIONS, { endpoint: subscription.endpoint }).catch(() => undefined);
      await this.sw.unsubscribe().catch(() => undefined);
    }
    this.subscribed.set(false);
  }

  private supported(): boolean {
    return this.sw.isEnabled && 'PushManager' in globalThis && 'Notification' in globalThis;
  }
}

function currentPermission(): NotificationPermission | null {
  return 'Notification' in globalThis ? Notification.permission : null;
}
