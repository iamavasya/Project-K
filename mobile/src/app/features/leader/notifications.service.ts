import { Injectable, effect, inject, signal } from '@angular/core';
import { AuthService } from '../../auth/auth.service';
import { Api } from '../../core/api';
import { AppNotification } from './leader.models';

/**
 * The inbox, as the web's notification.service: the latest notifications, the unread count the
 * bell shows, and marking read. No push yet (PLAN.md §8, phase 3): while the app is open the count
 * is read every minute, and at once when the person comes back to it from another app or tab.
 */
@Injectable({ providedIn: 'root' })
export class NotificationsService {
  /** How often an open, visible app asks for the count. */
  static readonly EVERY_MS = 60_000;

  private readonly api = inject(Api);
  private readonly auth = inject(AuthService);
  private readonly count = signal(0);
  private watching = false;

  constructor() {
    // Another account must not see the last one's badge.
    effect(() => {
      if (!this.auth.user()) this.count.set(0);
    });
  }

  /** What the bell's badge shows; the inbox keeps it in step as notifications are read. */
  readonly unread = this.count.asReadonly();

  /** Newest first; the API has no paging, only `take` (up to 100). */
  inbox(take: number): Promise<AppNotification[]> {
    return this.api.get<AppNotification[]>('notifications', { unreadOnly: false, take });
  }

  /**
   * Keeps the badge current from the first bell on: on coming back to the app (a resumed native
   * shell fires the same visibilitychange as a browser tab) and every minute while it shows.
   */
  watch(): void {
    if (this.watching) return;
    this.watching = true;
    const tick = () => {
      if (document.visibilityState === 'visible' && this.auth.user()) void this.refreshUnread();
    };
    document.addEventListener('visibilitychange', tick);
    setInterval(tick, NotificationsService.EVERY_MS);
  }

  async refreshUnread(): Promise<void> {
    try {
      this.count.set(await this.api.get<number>('notifications/unread-count'));
    } catch {
      // The badge keeps what it showed.
    }
  }

  async markRead(notificationKey: string): Promise<AppNotification> {
    const updated = await this.api.put<AppNotification>(`notifications/${notificationKey}/read`);
    this.count.update((count) => Math.max(0, count - 1));
    return updated;
  }

  async markAllRead(): Promise<void> {
    await this.api.put<number>('notifications/read-all');
    this.count.set(0);
  }
}
