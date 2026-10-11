import { Component, ElementRef, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  ActionSheetController,
  IonBackButton,
  IonBadge,
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
  IonSkeletonText,
  IonTitle,
  IonToolbar,
  ViewWillEnter,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { add } from 'ionicons/icons';
import { AuthService } from '../../auth/auth.service';
import { apiErrorText } from '../../core/api';
import { FAILED_TEXT, Loaded, valueOf } from '../../core/loaded';
import { Toasts } from '../../core/toast';
import { ForbiddenState, isForbidden } from '../account/forbidden';
import { PlanningSessionDto } from './planning.models';
import { canCreatePlanning, daysLabel, participantsLabel, windowLabel } from './planning.labels';
import { PlanningService } from './planning.service';
import { confirmPlanningDelete } from './planning-delete';

/**
 * «Планування таборів» (the web's planning-list): the kurin's sessions with their window and state,
 * readable by everyone in the kurin. «Нове планування» for those the web lets open one
 * (`canCreatePlanning`); delete, by a swipe, on the API's `canDelete`.
 */
@Component({
  selector: 'app-planning-list',
  imports: [
    ForbiddenState,
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
    IonList,
    IonItemGroup,
    IonItem,
    IonItemSliding,
    IonItemOptions,
    IonItemOption,
    IonLabel,
    IonBadge,
    IonSkeletonText,
  ],
  styles: `
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
      margin: 0 0 16px;
    }
    .note {
      margin: 6px 20px 24px;
      font-size: 13px;
      color: var(--lk-muted);
    }
    :host-context(.ios) .note {
      margin-inline: 32px;
    }
    ion-badge {
      flex: none;
      margin-inline-start: 8px;
    }
  `,
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/calendar" text="Календар" /></ion-buttons>
        <ion-title>Планування</ion-title>
        @if (canCreate && sessions().length) {
          <ion-buttons slot="end">
            <ion-button
              routerLink="/tabs/calendar/planning/new"
              aria-label="Нове планування"
              data-testid="new-planning"
            >
              <ion-icon slot="icon-only" name="add" />
            </ion-button>
          </ion-buttons>
        }
      </ion-toolbar>
    </ion-header>

    <ion-content [fullscreen]="true">
      <ion-refresher slot="fixed" (ionRefresh)="refresh($event)">
        <ion-refresher-content />
      </ion-refresher>
      <ion-header collapse="condense">
        <ion-toolbar><ion-title size="large">Планування</ion-title></ion-toolbar>
      </ion-header>

      @if (!kurinKey) {
        <p class="empty">Ти ще не в курені, тож планувань немає.</p>
      } @else {
        @switch (list().state) {
          @case ('loading') {
            <ion-list [inset]="true">
              <ion-item-group>
                <ion-item><ion-skeleton-text [animated]="true" style="height: 44px" /></ion-item>
                <ion-item><ion-skeleton-text [animated]="true" style="height: 44px" /></ion-item>
              </ion-item-group>
            </ion-list>
          }
          @case ('failed') {
            @if (forbidden()) {
              <app-forbidden />
            } @else {
              <p class="empty">{{ failedText }}</p>
            }
          }
          @default {
            @if (sessions().length) {
              <ion-list [inset]="true" data-testid="sessions">
                <ion-item-group>
                  @for (session of sessions(); track session.planningSessionKey) {
                    <ion-item-sliding
                      [disabled]="!session.canDelete || busy() !== null"
                      data-testid="session-row"
                    >
                      <ion-item
                        [button]="true"
                        [detail]="true"
                        [disabled]="busy() === session.planningSessionKey"
                        [routerLink]="['/tabs/calendar/planning', session.planningSessionKey]"
                      >
                        <ion-label class="ion-text-wrap">
                          <h3>{{ session.name }}</h3>
                          <p>{{ window(session) }}</p>
                          <p>{{ length(session.durationDays) }} · {{ people(session) }}</p>
                        </ion-label>
                        <ion-badge [class]="session.isCalculated ? 'lk-tag--info' : 'lk-tag--warn'">
                          {{ session.isCalculated ? 'Розраховано' : 'Нове' }}
                        </ion-badge>
                      </ion-item>
                      @if (session.canDelete) {
                        <ion-item-options side="end">
                          <ion-item-option
                            color="danger"
                            (click)="remove(session)"
                            data-testid="session-delete"
                            >Видалити</ion-item-option
                          >
                        </ion-item-options>
                      }
                    </ion-item-sliding>
                  }
                </ion-item-group>
              </ion-list>
              <p class="note">Дати табору, за яких найменше людей з КВ мають інші плани.</p>
            } @else {
              <div class="empty" data-testid="planning-empty">
                <h2>Календар ще спить</h2>
                <p>Перша сходина в плані — і він прокинеться.</p>
                @if (canCreate) {
                  <ion-button
                    size="default"
                    routerLink="/tabs/calendar/planning/new"
                    data-testid="new-planning"
                  >
                    <ion-icon slot="start" name="add" aria-hidden="true" />
                    Створити планування
                  </ion-button>
                }
              </div>
            }
          }
        }
      }
    </ion-content>
  `,
})
export class PlanningListPage implements OnInit, ViewWillEnter {
  private readonly planning = inject(PlanningService);
  private readonly auth = inject(AuthService);
  private readonly toasts = inject(Toasts);
  private readonly sheets = inject(ActionSheetController);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;

  protected readonly kurinKey = this.auth.user()?.kurinKey ?? null;
  protected readonly canCreate = canCreatePlanning(this.auth.user());
  protected readonly failedText = FAILED_TEXT;
  /** The API refused the read (another kurin's session): the web's «Немає доступу». */
  protected readonly forbidden = signal(false);
  protected readonly length = daysLabel;

  protected readonly list = signal<Loaded<PlanningSessionDto[]>>({ state: 'loading' });
  protected readonly sessions = computed(() => valueOf(this.list()) ?? []);
  /** The session a delete is running on. */
  protected readonly busy = signal<string | null>(null);
  private entered = false;

  constructor() {
    addIcons({ add });
  }

  ngOnInit(): void {
    void this.load();
  }

  /** Back from a new session or one deleted on its page: the list shows it. */
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
      this.list.set({ state: 'ready', value: await this.planning.sessions(this.kurinKey) });
    } catch (error) {
      this.forbidden.set(isForbidden(error));
      if (this.list().state !== 'ready') this.list.set({ state: 'failed' });
    }
  }

  protected window(session: PlanningSessionDto): string {
    return windowLabel(session);
  }

  protected people(session: PlanningSessionDto): string {
    return participantsLabel(session.participants.length);
  }

  protected async remove(session: PlanningSessionDto): Promise<void> {
    const list = this.host.querySelectorAll('ion-item-sliding');
    await Promise.all([...list].map((item) => (item as HTMLIonItemSlidingElement).close()));
    if (this.busy() || !(await confirmPlanningDelete(this.sheets, session))) return;
    this.busy.set(session.planningSessionKey);
    try {
      await this.planning.delete(session.planningSessionKey);
      await this.toasts.show('Планування видалено');
      await this.load();
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося видалити.'), 'danger');
    } finally {
      this.busy.set(null);
    }
  }
}
