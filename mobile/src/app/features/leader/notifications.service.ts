import { Injectable, inject, signal } from '@angular/core';
import { Api } from '../../core/api';
import { AppNotification } from './leader.models';

/**
 * The inbox, as the web's notification.service: the latest notifications, the unread count the
 * home bell shows, and marking read. No polling and no push; the count is read when Home shows.
 */
@Injectable({ providedIn: 'root' })
export class NotificationsService {
  private readonly api = inject(Api);
  private readonly count = signal(0);

  /** What the bell's badge shows; the inbox keeps it in step as notifications are read. */
  readonly unread = this.count.asReadonly();

  /** Newest first; the API has no paging, only `take` (up to 100). */
  inbox(take: number): Promise<AppNotification[]> {
    return this.api.get<AppNotification[]>('notifications', { unreadOnly: false, take });
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
