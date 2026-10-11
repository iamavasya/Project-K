import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  IonBackButton,
  IonButton,
  IonButtons,
  Config,
  IonContent,
  IonHeader,
  IonIcon,
  IonInfiniteScroll,
  IonInfiniteScrollContent,
  IonItem,
  IonItemGroup,
  IonLabel,
  IonList,
  IonRefresher,
  IonRefresherContent,
  IonSkeletonText,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { checkmarkDone } from 'ionicons/icons';
import { AuthService } from '../../auth/auth.service';
import { apiErrorText } from '../../core/api';
import { FAILED_TEXT, Loaded, settle, valueOf } from '../../core/loaded';
import { Toasts } from '../../core/toast';
import { dayLabel, timeLabel } from '../../me/labels';
import { AppNotification } from './leader.models';
import { mobileRouteOf } from './notification-route';
import { NotificationsService } from './notifications.service';

/** A page of the inbox; the API has `take` only (up to 100), so «more» reads a longer list. */
const PAGE = 20;
const MAX = 100;

/**
 * The web's notification bell as a screen: the latest notifications, unread ones marked, a tap
 * marks one read and opens what it is about when the phone has a screen for it.
 */
@Component({
  selector: 'app-notifications',
  imports: [
    IonHeader,
    IonToolbar,
    IonButtons,
    IonBackButton,
    IonButton,
    IonIcon,
    IonTitle,
    IonContent,
    IonRefresher,
    IonRefresherContent,
    IonList,
    IonItemGroup,
    IonItem,
    IonLabel,
    IonSkeletonText,
    IonInfiniteScroll,
    IonInfiniteScrollContent,
  ],
  styles: `
    .note {
      margin: 24px 20px;
      color: var(--lk-muted);
      text-align: center;
    }
    .dot {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      margin-inline-end: 12px;
      flex: none;
      align-self: flex-start;
      margin-top: 18px;
    }
    .dot.on {
      background: var(--lk-primary);
    }
    ion-label h3 {
      font-weight: 500;
      color: var(--lk-ink);
    }
    .unread ion-label h3 {
      font-weight: 700;
    }
    ion-label p.when {
      color: var(--lk-faint);
      margin-top: 2px;
    }
  `,
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/home" [text]="backText" /></ion-buttons>
        <ion-title>Сповіщення</ion-title>
        <ion-buttons slot="end">
          <ion-button
            aria-label="Позначити всі як прочитані"
            [disabled]="!hasUnread() || markingAll()"
            (click)="markAll()"
          >
            <ion-icon slot="icon-only" name="checkmark-done" aria-hidden="true" />
          </ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content [fullscreen]="true">
      <ion-refresher slot="fixed" (ionRefresh)="refresh($event)">
        <ion-refresher-content />
      </ion-refresher>

      @switch (inbox().state) {
        @case ('loading') {
          <ion-list [inset]="true">
            <ion-item-group>
              @for (row of [1, 2, 3]; track row) {
                <ion-item>
                  <ion-label>
                    <ion-skeleton-text [animated]="true" style="width: 60%" />
                    <ion-skeleton-text [animated]="true" style="width: 90%" />
                  </ion-label>
                </ion-item>
              }
            </ion-item-group>
          </ion-list>
        }
        @case ('failed') {
          <p class="note">{{ failedText }}</p>
        }
        @default {
          @if (items().length) {
            <ion-list [inset]="true">
              <ion-item-group>
                @for (item of items(); track item.notificationKey) {
                  <ion-item
                    [button]="true"
                    [detail]="routeOf(item) !== null"
                    [class.unread]="!item.isRead"
                    [disabled]="opening() === item.notificationKey"
                    (click)="open(item)"
                    data-testid="notification"
                  >
                    <span class="dot" [class.on]="!item.isRead" slot="start" aria-hidden="true"></span>
                    <ion-label class="ion-text-wrap">
                      <h3>{{ item.title }}</h3>
                      @if (item.body) { <p>{{ item.body }}</p> }
                      <p class="when">{{ when(item) }}{{ item.isRead ? '' : ' · нове' }}</p>
                    </ion-label>
                  </ion-item>
                }
              </ion-item-group>
            </ion-list>
          } @else {
            <p class="note">Сповіщень немає.</p>
          }
        }
      }

      <ion-infinite-scroll [disabled]="!hasMore()" (ionInfinite)="more($event)">
        <ion-infinite-scroll-content />
      </ion-infinite-scroll>
    </ion-content>
  `,
})
export class NotificationsPage implements OnInit {
  private readonly notifications = inject(NotificationsService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toasts = inject(Toasts);

  protected readonly failedText = FAILED_TEXT;
  /** iOS names where back leads; Material shows the arrow alone. */
  protected readonly backText = inject(Config).get('mode') === 'ios' ? 'Головна' : undefined;
  protected readonly inbox = signal<Loaded<AppNotification[]>>({ state: 'loading' });
  protected readonly opening = signal<string | null>(null);
  protected readonly markingAll = signal(false);
  private readonly take = signal(PAGE);

  protected readonly items = computed(() => valueOf(this.inbox()) ?? []);
  protected readonly hasUnread = computed(() => this.items().some((item) => !item.isRead));
  /** A full page back means there may be more; the API stops at 100. */
  protected readonly hasMore = computed(() => this.items().length >= this.take() && this.take() < MAX);

  constructor() {
    addIcons({ checkmarkDone });
  }

  ngOnInit(): void {
    void this.load();
  }

  protected routeOf(item: AppNotification): string | null {
    return mobileRouteOf(item, this.auth.user()?.kurinKey ?? null);
  }

  protected when(item: AppNotification): string {
    const date = new Date(item.createdAtUtc);
    return `${dayLabel(date, new Date())}, ${timeLabel(date, null, false)}`;
  }

  protected async refresh(event: Event): Promise<void> {
    await this.load();
    await (event.target as HTMLIonRefresherElement).complete();
  }

  protected async more(event: Event): Promise<void> {
    this.take.update((take) => Math.min(MAX, take + PAGE));
    await this.load();
    await (event.target as HTMLIonInfiniteScrollElement).complete();
  }

  /** Read first, as the web does; the screen opens even if marking it failed. */
  protected async open(item: AppNotification): Promise<void> {
    if (this.opening()) return;
    const route = this.routeOf(item);
    if (!item.isRead) {
      this.opening.set(item.notificationKey);
      try {
        const updated = await this.notifications.markRead(item.notificationKey);
        this.replace({ ...item, ...updated, isRead: true });
      } catch {
        // Marked read next time; the tap still leads where it points.
      } finally {
        this.opening.set(null);
      }
    }
    if (route) await this.router.navigateByUrl(route);
  }

  protected async markAll(): Promise<void> {
    this.markingAll.set(true);
    try {
      await this.notifications.markAllRead();
      const readAtUtc = new Date().toISOString();
      this.inbox.set({
        state: 'ready',
        value: this.items().map((item) => ({ ...item, isRead: true, readAtUtc: item.readAtUtc ?? readAtUtc })),
      });
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося позначити. Спробуй ще раз.'), 'danger');
    } finally {
      this.markingAll.set(false);
    }
  }

  private async load(): Promise<void> {
    await Promise.all([settle(this.notifications.inbox(this.take()), this.inbox), this.notifications.refreshUnread()]);
  }

  private replace(updated: AppNotification): void {
    this.inbox.set({
      state: 'ready',
      value: this.items().map((item) => (item.notificationKey === updated.notificationKey ? updated : item)),
    });
  }
}
