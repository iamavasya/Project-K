import { Component, ElementRef, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  ActionSheetController,
  Config,
  IonBackButton,
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonIcon,
  IonItem,
  IonItemGroup,
  IonItemOption,
  IonItemOptions,
  IonItemSliding,
  IonLabel,
  IonList,
  IonRefresher,
  IonRefresherContent,
  IonSearchbar,
  IonSkeletonText,
  IonSpinner,
  IonTitle,
  IonToolbar,
  ViewWillEnter,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { ellipsisHorizontal } from 'ionicons/icons';
import { AuthService } from '../../auth/auth.service';
import { apiErrorText } from '../../core/api';
import { FAILED_TEXT, Loaded, valueOf } from '../../core/loaded';
import { Toasts } from '../../core/toast';
import { confirmAgendaDelete } from '../agenda/agenda-item-view';
import { AgendaArchivePage, AgendaItemDto } from '../agenda/agenda.models';
import { archivePolicyLabel, archivedLabel, purgeDate, shortStamp, targetsLabel } from '../agenda/agenda.labels';
import { AgendaService } from '../agenda/agenda.service';

const PAGE = 20;

/**
 * The board's archive (the web's agenda-board archive view): archived tasks newest first, searched
 * on the server, with the kurin's rules for how long they stay. Anyone who sees a task sees it here;
 * «Повернути» and «Видалити назавжди» follow the API's `canEdit`, as on the web.
 */
@Component({
  selector: 'app-tasks-archive',
  imports: [
    RouterLink,
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
    IonSearchbar,
    IonList,
    IonItemGroup,
    IonItem,
    IonItemSliding,
    IonItemOptions,
    IonItemOption,
    IonLabel,
    IonSkeletonText,
    IonSpinner,
  ],
  styles: `
    .search {
      padding: 0 8px;
    }
    :host-context(.md) .search {
      padding: 8px 12px 0;
    }
    .policy {
      margin: 4px 20px 0;
      font-size: 13px;
      color: var(--lk-muted);
    }
    :host-context(.ios) .policy {
      margin-inline: 32px;
    }
    .empty {
      margin: 24px 20px 0;
      color: var(--lk-muted);
    }
    :host-context(.ios) .empty {
      margin-inline: 32px;
    }
    .empty h2 {
      margin: 0 0 4px;
      font-size: 17px;
      font-weight: 600;
      color: var(--lk-ink);
    }
    .empty p {
      margin: 0;
    }
    .actions {
      margin: 0 -8px 0 4px;
      --padding-start: 10px;
      --padding-end: 10px;
      min-height: 44px;
    }
    .more {
      display: flex;
      justify-content: center;
      padding: 0 16px 24px;
    }
  `,
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/tasks" [text]="backText" /></ion-buttons>
        <ion-title>Архів задач</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content [fullscreen]="true">
      <ion-refresher slot="fixed" (ionRefresh)="refresh($event)">
        <ion-refresher-content />
      </ion-refresher>
      <ion-header collapse="condense">
        <ion-toolbar><ion-title size="large">Архів задач</ion-title></ion-toolbar>
      </ion-header>

      @if (!kurinKey) {
        <p class="empty">Ти ще не в курені, тож архіву немає.</p>
      } @else {
        <div class="search">
          <ion-searchbar
            placeholder="Пошук в архіві"
            [debounce]="300"
            [value]="search()"
            (ionInput)="setSearch($event)"
            data-testid="archive-search"
          />
        </div>
        @if (policy(); as rules) { <p class="policy" data-testid="archive-policy">{{ rules }}</p> }

        @switch (archive().state) {
          @case ('loading') {
            <ion-list [inset]="true">
              <ion-item-group>
                <ion-item><ion-skeleton-text [animated]="true" style="height: 44px" /></ion-item>
                <ion-item><ion-skeleton-text [animated]="true" style="height: 44px" /></ion-item>
              </ion-item-group>
            </ion-list>
          }
          @case ('failed') { <p class="empty">{{ failedText }}</p> }
          @default {
            @if (items().length) {
              <ion-list [inset]="true" data-testid="archive">
                <ion-item-group>
                  @for (task of items(); track task.agendaItemKey) {
                    <ion-item-sliding [disabled]="!task.canEdit || busy() !== null" data-testid="archive-row">
                      <ion-item [button]="true" [detail]="!task.canEdit" [routerLink]="['/tabs/tasks/task', task.agendaItemKey]">
                        <ion-label class="ion-text-wrap">
                          <h3>{{ task.title }}</h3>
                          @if (targets(task); as line) { <p>{{ line }}</p> }
                          <p>{{ archived(task) }}</p>
                          @if (purge(task); as when) { <p>Видалиться {{ when }}</p> }
                        </ion-label>
                        @if (task.canEdit) {
                          <ion-button
                            slot="end"
                            fill="clear"
                            size="default"
                            class="actions"
                            [disabled]="busy() !== null"
                            [attr.aria-label]="'Дії із задачею «' + task.title + '»'"
                            (click)="$event.stopPropagation(); $event.preventDefault(); actions(task)"
                            data-testid="archive-actions"
                          >
                            @if (busy() === task.agendaItemKey) {
                              <ion-spinner name="crescent" />
                            } @else {
                              <ion-icon slot="icon-only" name="ellipsis-horizontal" aria-hidden="true" />
                            }
                          </ion-button>
                        }
                      </ion-item>
                      @if (task.canEdit) {
                        <ion-item-options side="end">
                          <ion-item-option color="primary" (click)="restore(task)">Повернути</ion-item-option>
                          <ion-item-option color="danger" (click)="remove(task)">Видалити</ion-item-option>
                        </ion-item-options>
                      }
                    </ion-item-sliding>
                  }
                </ion-item-group>
              </ion-list>
              @if (items().length < total()) {
                <div class="more">
                  <ion-button fill="clear" size="default" [disabled]="loadingMore()" (click)="loadMore()" data-testid="archive-more">
                    @if (loadingMore()) { <ion-spinner name="crescent" /> } @else {
                      Завантажити ще ({{ total() - items().length }})
                    }
                  </ion-button>
                </div>
              }
            } @else if (search().trim()) {
              <div class="empty">
                <h2>Нічого не знайдено</h2>
                <p>Жодна задача в архіві не підходить під пошук.</p>
              </div>
            } @else {
              <div class="empty" data-testid="archive-empty">
                <h2>Архів порожній</h2>
                <p>Сюди потрапляють зроблені задачі — самі чи кнопкою «В архів» на задачі.</p>
              </div>
            }
          }
        }
      }
    </ion-content>
  `,
})
export class TasksArchivePage implements OnInit, ViewWillEnter {
  private readonly agenda = inject(AgendaService);
  private readonly auth = inject(AuthService);
  private readonly toasts = inject(Toasts);
  private readonly sheets = inject(ActionSheetController);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;

  protected readonly kurinKey = this.auth.user()?.kurinKey ?? null;
  protected readonly failedText = FAILED_TEXT;
  /** iOS names where back leads; Material shows the arrow alone. */
  protected readonly backText = inject(Config).get('mode') === 'ios' ? 'Задачі' : undefined;
  protected readonly targets = targetsLabel;
  protected readonly archived = archivedLabel;

  protected readonly search = signal('');
  protected readonly archive = signal<Loaded<AgendaArchivePage>>({ state: 'loading' });
  protected readonly loadingMore = signal(false);
  /** The task a restore or delete is running on. */
  protected readonly busy = signal<string | null>(null);

  protected readonly items = computed(() => valueOf(this.archive())?.items ?? []);
  protected readonly total = computed(() => valueOf(this.archive())?.total ?? 0);
  protected readonly policy = computed(() => {
    const page = valueOf(this.archive());
    return page ? archivePolicyLabel(page.policy) : null;
  });

  private entered = false;

  constructor() {
    addIcons({ ellipsisHorizontal });
  }

  ngOnInit(): void {
    void this.load();
  }

  /** Back from a task that was restored or deleted there: it is gone from here too. */
  ionViewWillEnter(): void {
    if (this.entered) void this.load();
    this.entered = true;
  }

  protected async refresh(event: Event): Promise<void> {
    await this.load();
    await (event.target as HTMLIonRefresherElement).complete();
  }

  private async load(): Promise<void> {
    if (!this.kurinKey) return;
    try {
      const page = await this.agenda.archive(this.kurinKey, this.search().trim() || null, 0, PAGE);
      this.archive.set({ state: 'ready', value: page });
    } catch {
      if (this.archive().state !== 'ready') this.archive.set({ state: 'failed' });
    }
  }

  protected async loadMore(): Promise<void> {
    const shown = valueOf(this.archive());
    if (this.loadingMore() || !this.kurinKey || !shown) return;
    this.loadingMore.set(true);
    try {
      const page = await this.agenda.archive(this.kurinKey, this.search().trim() || null, shown.items.length, PAGE);
      const keys = new Set(shown.items.map((i) => i.agendaItemKey));
      this.archive.set({
        state: 'ready',
        value: { ...page, items: [...shown.items, ...page.items.filter((i) => !keys.has(i.agendaItemKey))] },
      });
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося завантажити.'), 'danger');
    } finally {
      this.loadingMore.set(false);
    }
  }

  protected setSearch(event: Event): void {
    this.search.set(String((event as CustomEvent<{ value?: string | null }>).detail.value ?? ''));
    void this.load();
  }

  protected purge(task: AgendaItemDto): string | null {
    const date = purgeDate(task, valueOf(this.archive())?.policy.purgeAfterDays ?? null);
    return date ? shortStamp(date) : null;
  }

  /** The row's «…»: the same two actions the web shows on an archived task. */
  protected async actions(task: AgendaItemDto): Promise<void> {
    const sheet = await this.sheets.create({
      header: `«${task.title}»`,
      buttons: [
        { text: 'Повернути на дошку', data: 'restore' },
        { text: 'Видалити назавжди', role: 'destructive', data: 'delete' },
        { text: 'Скасувати', role: 'cancel' },
      ],
    });
    await sheet.present();
    const { data } = await sheet.onWillDismiss<string>();
    if (data === 'restore') await this.restore(task);
    if (data === 'delete') await this.remove(task);
  }

  protected async restore(task: AgendaItemDto): Promise<void> {
    await this.closeSliding();
    if (this.busy()) return;
    this.busy.set(task.agendaItemKey);
    try {
      await this.agenda.setArchived(task.agendaItemKey, false);
      await this.toasts.show('Повернено на дошку');
      await this.load();
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося повернути.'), 'danger');
    } finally {
      this.busy.set(null);
    }
  }

  protected async remove(task: AgendaItemDto): Promise<void> {
    await this.closeSliding();
    if (this.busy() || !(await confirmAgendaDelete(this.sheets, task))) return;
    this.busy.set(task.agendaItemKey);
    try {
      await this.agenda.delete(task.agendaItemKey);
      await this.toasts.show('Видалено');
      await this.load();
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося видалити.'), 'danger');
    } finally {
      this.busy.set(null);
    }
  }

  private async closeSliding(): Promise<void> {
    const list = this.host.querySelectorAll('ion-item-sliding');
    await Promise.all([...list].map((item) => (item as HTMLIonItemSlidingElement).close()));
  }
}
