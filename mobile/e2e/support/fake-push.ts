import type { Page } from '@playwright/test';

/**
 * The browser's side of Web Push, faked in the page before the app starts. Playwright cannot reach a
 * real push service (headless Chromium has no FCM, WebKit has no Push API at all), so this stands
 * in: a service worker container already in charge, a PushManager whose subscriptions survive a
 * reload, and the Notification permission with the person's answer.
 */
export interface FakeOptions {
  /** What Notification.permission says at first. */
  permission: NotificationPermission;
  /** What the person answers when asked. */
  answer?: NotificationPermission;
  /** No Push API in this browser. */
  noPushApi?: boolean;
}

/** The browser's push machinery, faked in the page before the app starts. Survives a reload. */
export async function fakePush(page: Page, options: FakeOptions): Promise<void> {
  await page.addInitScript((o: FakeOptions) => {
    const stored = 'e2e-push-endpoint';
    const savedPermission = sessionStorage.getItem('e2e-push-permission') as NotificationPermission | null;
    let permission: NotificationPermission = savedPermission ?? o.permission;

    class FakeSubscription {
      readonly expirationTime = null;
      constructor(
        readonly endpoint: string,
        readonly options: { applicationServerKey: ArrayBuffer | null; userVisibleOnly: boolean },
      ) {}
      getKey(): ArrayBuffer | null {
        return null;
      }
      toJSON() {
        return {
          endpoint: this.endpoint,
          expirationTime: null,
          keys: {
            p256dh: 'BCVxsr7N_eNgVRqvHtD0zTZsEc6-VV-JvLexhqUzORcxaOzi6-AYWXvTBHm4bjyPjs7Vd8pZGH6SRpkNtoIAiw4',
            auth: 'BTBZMqHH6r4Tts7J_aSIgg',
          },
        };
      }
      unsubscribe(): Promise<boolean> {
        localStorage.removeItem(stored);
        current = null;
        return Promise.resolve(true);
      }
    }

    const endpoint = localStorage.getItem(stored);
    let current: FakeSubscription | null = endpoint
      ? new FakeSubscription(endpoint, { applicationServerKey: null, userVisibleOnly: true })
      : null;

    const pushManager = {
      getSubscription: () => Promise.resolve(current),
      permissionState: () => Promise.resolve(permission === 'default' ? 'prompt' : permission),
      subscribe: (request: { applicationServerKey: Uint8Array }) => {
        if (permission !== 'granted') return Promise.reject(new DOMException('Permission denied', 'NotAllowedError'));
        const made = `https://fcm.googleapis.com/fcm/send/e2e-${crypto.randomUUID()}`;
        localStorage.setItem(stored, made);
        current = new FakeSubscription(made, {
          applicationServerKey: request.applicationServerKey.slice().buffer,
          userVisibleOnly: true,
        });
        return Promise.resolve(current);
      },
    };
    const noop = () => undefined;
    const registration = {
      scope: new URL('/m/', location.href).href,
      pushManager,
      active: { state: 'activated' },
      showNotification: () => Promise.resolve(),
      getNotifications: () => Promise.resolve([]),
      update: () => Promise.resolve(),
      addEventListener: noop,
      removeEventListener: noop,
    };
    const container = {
      controller: { state: 'activated', scriptURL: new URL('/m/ngsw-worker.js', location.href).href, postMessage: noop },
      ready: Promise.resolve(registration),
      register: () => Promise.resolve(registration),
      getRegistration: () => Promise.resolve(registration),
      getRegistrations: () => Promise.resolve([registration]),
      startMessages: noop,
      addEventListener: noop,
      removeEventListener: noop,
    };
    Object.defineProperty(navigator, 'serviceWorker', { configurable: true, get: () => container });

    const w = window as unknown as Record<string, unknown>;
    if (o.noPushApi) {
      delete w['PushManager'];
    } else if (!('PushManager' in window)) {
      w['PushManager'] = function PushManager() {};
    }
    const FakeNotification = function Notification() {} as unknown as { permission: NotificationPermission; requestPermission: () => Promise<NotificationPermission> };
    Object.defineProperty(FakeNotification, 'permission', { get: () => permission });
    FakeNotification.requestPermission = () => {
      if (permission === 'default') {
        permission = o.answer ?? 'granted';
        sessionStorage.setItem('e2e-push-permission', permission);
      }
      return Promise.resolve(permission);
    };
    Object.defineProperty(window, 'Notification', { configurable: true, writable: true, value: FakeNotification });
  }, options);
}

/** Opened from the home screen, as iOS and Chrome report it. */
export async function standalone(page: Page): Promise<void> {
  await page.addInitScript(() => {
    Object.defineProperty(navigator, 'standalone', { value: true });
    const original = window.matchMedia.bind(window);
    window.matchMedia = (query: string) =>
      query.includes('display-mode: standalone')
        ? ({ ...original(query), matches: true, media: query } as MediaQueryList)
        : original(query);
  });
}
