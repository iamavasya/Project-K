import { Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  ActionSheetController,
  AlertController,
  IonBackButton,
  IonBadge,
  IonButtons,
  IonContent,
  IonHeader,
  IonItem,
  IonItemGroup,
  IonLabel,
  IonList,
  IonListHeader,
  IonRefresher,
  IonRefresherContent,
  IonSkeletonText,
  IonTitle,
  IonToolbar,
} from '@ionic/angular';
import { AuthService } from '../../auth/auth.service';
import { apiErrorText } from '../../core/api';
import { FAILED_TEXT, Loaded, valueOf } from '../../core/loaded';
import { Toasts } from '../../core/toast';
import { ForbiddenState, isForbidden } from '../account/forbidden';
import { shortDate, utcDate } from '../kurin/kurin.labels';
import {
  WAITLIST_STATUS,
  WaitlistEntry,
  canDecide,
  canResend,
  waitlistLine,
  waitlistSections,
  waitlistStatus,
} from './waitlist.labels';
import { WaitlistApi } from './waitlist.service';

/**
 * «Заявки» (web adminModule waitlist-management): applications to join, for system admins. A fresh
 * one is approved (the invitation goes out) or rejected with an optional note; an approved one can
 * have its invitation sent again. The web reaches it from the admin panel; here it is a row in
 * «Меню» and where a `WaitlistEntrySubmitted` notification leads.
 */
@Component({
  selector: 'app-waitlist',
  imports: [
    IonHeader,
    IonToolbar,
    IonButtons,
    IonBackButton,
    IonTitle,
    IonContent,
    IonRefresher,
    IonRefresherContent,
    IonList,
    IonListHeader,
    IonItemGroup,
    IonItem,
    IonLabel,
    IonBadge,
    IonSkeletonText,
    ForbiddenState,
  ],
  styles: `
    .note {
      margin: 24px 20px;
      color: var(--lk-muted);
      text-align: center;
    }
    ion-label h3 {
      font-weight: 600;
      color: var(--lk-ink);
    }
    /* A long address has no spaces to wrap at. */
    ion-label p {
      overflow-wrap: anywhere;
    }
    ion-badge {
      flex: none;
      align-self: flex-start;
      margin-top: 12px;
    }
  `,
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/more" text="" /></ion-buttons>
        <ion-title>Заявки</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content [fullscreen]="true">
      <ion-refresher slot="fixed" [disabled]="!isAdmin() || forbidden()" (ionRefresh)="refresh($event)">
        <ion-refresher-content />
      </ion-refresher>
      <ion-header collapse="condense">
        <ion-toolbar><ion-title size="large">Заявки</ion-title></ion-toolbar>
      </ion-header>

      @if (!isAdmin() || forbidden()) {
        <app-forbidden message="Заявки на приєднання розглядає адміністратор системи." />
      } @else {
        @switch (list().state) {
          @case ('loading') {
            <ion-list [inset]="true">
              <ion-item-group>
                @for (row of [1, 2, 3]; track row) {
                  <ion-item>
                    <ion-label>
                      <ion-skeleton-text [animated]="true" style="width: 50%" />
                      <ion-skeleton-text [animated]="true" style="width: 70%" />
                    </ion-label>
                  </ion-item>
                }
              </ion-item-group>
            </ion-list>
          }
          @case ('failed') { <p class="note">{{ failedText }}</p> }
          @default {
            @for (section of sections(); track section.key) {
              <ion-list [inset]="true" [attr.data-testid]="'waitlist-' + section.key">
                <ion-list-header><ion-label>{{ section.title }} · {{ section.entries.length }}</ion-label></ion-list-header>
                <ion-item-group>
                  @for (entry of section.entries; track entry.waitlistEntryKey) {
                    <ion-item
                      [button]="actionable(entry)"
                      [detail]="false"
                      [disabled]="busy() === entry.waitlistEntryKey"
                      (click)="actionable(entry) && act(entry)"
                      data-testid="waitlist-entry"
                    >
                      <ion-label class="ion-text-wrap">
                        <h3>{{ entry.firstName }} {{ entry.lastName }}</h3>
                        <p>{{ entry.email }}</p>
                        <p>{{ line(entry) }}</p>
                        <p>Подано {{ day(entry.requestedAtUtc) }}</p>
                      </ion-label>
                      @if (status(entry); as s) {
                        <ion-badge slot="end" [class]="'lk-tag--' + s.tone">{{ s.label }}</ion-badge>
                      }
                    </ion-item>
                  }
                </ion-item-group>
              </ion-list>
            } @empty {
              <p class="note" data-testid="waitlist-empty">Заявок немає. Нові заявки на приєднання зʼявляться тут.</p>
            }
          }
        }
      }
    </ion-content>
  `,
})
export class WaitlistPage implements OnInit {
  private readonly data = inject(WaitlistApi);
  private readonly auth = inject(AuthService);
  private readonly sheets = inject(ActionSheetController);
  private readonly alerts = inject(AlertController);
  private readonly toasts = inject(Toasts);

  protected readonly failedText = FAILED_TEXT;
  protected readonly line = waitlistLine;
  protected readonly isAdmin = computed(() => this.auth.user()?.isAdmin ?? false);
  protected readonly list = signal<Loaded<WaitlistEntry[]>>({ state: 'loading' });
  protected readonly forbidden = signal(false);
  protected readonly busy = signal<string | null>(null);
  protected readonly sections = computed(() => waitlistSections(valueOf(this.list()) ?? []));

  ngOnInit(): void {
    if (this.isAdmin()) void this.load();
  }

  protected async refresh(event: Event): Promise<void> {
    await this.load();
    await (event.target as HTMLIonRefresherElement).complete();
  }

  protected actionable(entry: WaitlistEntry): boolean {
    return canDecide(entry) || canResend(entry);
  }

  protected status(entry: WaitlistEntry) {
    const status = waitlistStatus(entry);
    return status ? WAITLIST_STATUS[status] : null;
  }

  protected day(utc: string): string {
    return shortDate(utcDate(utc));
  }

  /** The web's row buttons as one sheet; the sheet itself says what a choice sends. */
  protected async act(entry: WaitlistEntry): Promise<void> {
    if (this.busy()) return;
    const name = `${entry.firstName} ${entry.lastName}`;
    const decide = canDecide(entry);
    const sheet = await this.sheets.create({
      header: name,
      subHeader: decide
        ? `Схвалення надішле на ${entry.email} лист із запрошенням.`
        : `Нове запрошення піде на ${entry.email}; попереднє посилання перестане працювати.`,
      buttons: decide
        ? [
            { text: 'Схвалити й надіслати запрошення', data: 'approve' },
            { text: 'Відхилити', role: 'destructive', data: 'reject' },
            { text: 'Скасувати', role: 'cancel' },
          ]
        : [
            { text: 'Надіслати запрошення ще раз', data: 'resend' },
            { text: 'Скасувати', role: 'cancel' },
          ],
    });
    await sheet.present();
    const { data } = await sheet.onWillDismiss<string>();
    if (data === 'approve') {
      await this.run(entry, () => this.data.approve(entry.waitlistEntryKey), 'Схвалено. Запрошення надіслано.', 'Не вдалося схвалити.');
    } else if (data === 'reject') {
      await this.reject(entry, name);
    } else if (data === 'resend') {
      await this.run(entry, () => this.data.resendInvitation(entry.waitlistEntryKey), 'Запрошення надіслано ще раз.', 'Не вдалося надіслати.');
    }
  }

  /** As the web's dialog: the name to confirm and an optional reason. */
  private async reject(entry: WaitlistEntry, name: string): Promise<void> {
    const alert = await this.alerts.create({
      header: 'Відхилити заявку',
      message: `Відхилити заявку ${name}?`,
      inputs: [
        {
          name: 'note',
          type: 'textarea',
          placeholder: 'Причина відмови (не обовʼязково)',
          attributes: { maxlength: 1000, 'aria-label': 'Причина відмови' },
        },
      ],
      buttons: [
        { text: 'Скасувати', role: 'cancel' },
        { text: 'Відхилити', role: 'destructive' },
      ],
    });
    await alert.present();
    const { role, data } = await alert.onDidDismiss<{ values?: { note?: string } }>();
    if (role !== 'destructive') return;
    const note = data?.values?.note?.trim() || null;
    await this.run(entry, () => this.data.reject(entry.waitlistEntryKey, note), 'Заявку відхилено.', 'Не вдалося відхилити.');
  }

  private async run(entry: WaitlistEntry, call: () => Promise<unknown>, done: string, failed: string): Promise<void> {
    this.busy.set(entry.waitlistEntryKey);
    try {
      await call();
      await this.toasts.show(done);
      await this.load();
    } catch (error) {
      await this.toasts.show(apiErrorText(error, `${failed} Спробуй ще раз.`), 'danger');
    } finally {
      this.busy.set(null);
    }
  }

  private async load(): Promise<void> {
    try {
      this.list.set({ state: 'ready', value: await this.data.entries() });
      this.forbidden.set(false);
    } catch (error) {
      if (isForbidden(error)) this.forbidden.set(true);
      else if (this.list().state !== 'ready') this.list.set({ state: 'failed' });
    }
  }
}
