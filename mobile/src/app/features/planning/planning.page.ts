import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import {
  ActionSheetController,
  Config,
  IonBackButton,
  IonBadge,
  IonButtons,
  IonContent,
  IonHeader,
  IonIcon,
  IonItem,
  IonItemGroup,
  IonLabel,
  IonList,
  IonListHeader,
  IonRefresher,
  IonRefresherContent,
  IonSkeletonText,
  IonSpinner,
  IonTitle,
  IonToolbar,
  NavController,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { calendarOutline, peopleOutline, timeOutline } from 'ionicons/icons';
import { AuthService } from '../../auth/auth.service';
import { apiErrorText } from '../../core/api';
import { FAILED_TEXT, Loaded, valueOf } from '../../core/loaded';
import { Toasts } from '../../core/toast';
import { ForbiddenState, isForbidden } from '../account/forbidden';
import { canManageAgenda } from '../agenda/agenda.labels';
import { AgendaService } from '../agenda/agenda.service';
import { AvailabilityStrip } from './availability-strip';
import { confirmPlanningDelete } from './planning-delete';
import { PlanningParticipantDto, PlanningSessionDto } from './planning.models';
import {
  busyDuringCamp,
  calendarRequest,
  dayRange,
  daysLabel,
  optimalDays,
  participantsLabel,
  spanDays,
  weightLabel,
  windowLabel,
} from './planning.labels';
import { PlanningService } from './planning.service';

/**
 * One planning session (the web's planning-detail dialog, as a page): the search window, the
 * chosen dates with who is still busy then, the availability strip and each person's busy days.
 * «Перенести в календар» makes the kurin-wide event the web makes; delete follows `canDelete`.
 */
@Component({
  selector: 'app-planning',
  imports: [
    ForbiddenState,
    AvailabilityStrip,
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
    IonIcon,
    IonBadge,
    IonSkeletonText,
    IonSpinner,
  ],
  styles: `
    .head {
      padding: 8px 20px 4px;
    }
    :host-context(.ios) .head {
      padding-inline: 16px;
    }
    h1 {
      margin: 0 0 6px;
      font-size: 24px;
      line-height: 30px;
      font-weight: 700;
      color: var(--lk-ink);
      overflow-wrap: anywhere;
    }
    .row-icon {
      color: var(--lk-muted);
      font-size: 20px;
    }
    .best h2 {
      font-size: 22px;
      line-height: 28px;
      font-weight: 700;
      color: var(--lk-primary-700);
    }
    .section {
      margin: 20px 20px 8px;
      font-size: 13px;
      color: var(--lk-muted);
    }
    :host-context(.ios) .section {
      margin-inline: 36px;
      font-size: 16px;
      font-weight: 600;
      color: var(--ion-color-medium-tint);
    }
    :host-context(.md) .section {
      margin-inline: 33px;
      color: var(--lk-faint);
      font-size: 11px;
      font-weight: 700;
      letter-spacing: 0.18em;
      text-transform: uppercase;
    }
    .hint {
      margin: 6px 20px 0;
      font-size: 13px;
      color: var(--lk-muted);
    }
    :host-context(.ios) .hint {
      margin-inline: 32px;
    }
    .center {
      text-align: center;
    }
    .gap {
      height: 8px;
    }
  `,
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/calendar/planning" [text]="backText" /></ion-buttons>
        <ion-title>Деталі</ion-title>
      </ion-toolbar>
    </ion-header>

    <ion-content [fullscreen]="true">
      <ion-refresher slot="fixed" (ionRefresh)="refresh($event)">
        <ion-refresher-content />
      </ion-refresher>

      @switch (state().state) {
        @case ('loading') {
          <div class="head">
            <ion-skeleton-text [animated]="true" style="width: 70%; height: 28px" />
          </div>
          <ion-list [inset]="true">
            <ion-item-group>
              <ion-item><ion-skeleton-text [animated]="true" style="height: 40px" /></ion-item>
              <ion-item><ion-skeleton-text [animated]="true" style="height: 40px" /></ion-item>
            </ion-item-group>
          </ion-list>
        }
        @case ('failed') {
          @if (forbidden()) {
            <app-forbidden />
          } @else {
            <ion-list [inset]="true">
              <ion-item-group>
                <ion-item><ion-label class="ion-text-wrap">{{ failedText }}</ion-label></ion-item>
              </ion-item-group>
            </ion-list>
          }
        }
        @default {
          @if (session(); as s) {
            <div class="head">
              <h1 data-testid="planning-name">{{ s.name }}</h1>
              <ion-badge [class]="s.isCalculated ? 'lk-tag--info' : 'lk-tag--warn'">
                {{ s.isCalculated ? 'Розраховано' : 'Нове' }}
              </ion-badge>
            </div>

            <ion-list [inset]="true">
              <ion-item-group>
                <ion-item>
                  <ion-icon
                    slot="start"
                    class="row-icon"
                    name="calendar-outline"
                    aria-hidden="true"
                  />
                  <ion-label class="ion-text-wrap">
                    <h3 data-testid="planning-window">{{ window() }}</h3>
                    <p>Період пошуку</p>
                  </ion-label>
                </ion-item>
                <ion-item>
                  <ion-icon slot="start" class="row-icon" name="time-outline" aria-hidden="true" />
                  <ion-label class="ion-text-wrap">
                    <h3>{{ length() }}</h3>
                    <p>Тривалість табору</p>
                  </ion-label>
                </ion-item>
                <ion-item>
                  <ion-icon
                    slot="start"
                    class="row-icon"
                    name="people-outline"
                    aria-hidden="true"
                  />
                  <ion-label class="ion-text-wrap">
                    <h3>{{ people() }}</h3>
                    <p>Учасники КВ</p>
                  </ion-label>
                </ion-item>
              </ion-item-group>
            </ion-list>

            @if (best(); as dates) {
              <ion-list [inset]="true" class="best" data-testid="optimal">
                <ion-list-header><ion-label>Оптимальна дата</ion-label></ion-list-header>
                <ion-item-group>
                  <ion-item>
                    <ion-label class="ion-text-wrap">
                      <h2 data-testid="optimal-dates">{{ dates }}</h2>
                      <p>{{ busyLine() }}</p>
                    </ion-label>
                  </ion-item>
                  @if (canTransfer) {
                    <ion-item
                      [button]="true"
                      [detail]="false"
                      [disabled]="adding() || added()"
                      (click)="addToCalendar()"
                      data-testid="to-calendar"
                    >
                      <ion-label class="center" color="primary">
                        @if (adding()) {
                          <ion-spinner name="crescent" />
                        } @else {
                          {{ added() ? 'Додано в календар куреня' : 'Перенести в календар' }}
                        }
                      </ion-label>
                    </ion-item>
                  }
                </ion-item-group>
              </ion-list>
            } @else {
              <p class="hint">Дати ще не пораховано.</p>
            }

            <h2 class="section">Графік зайнятості</h2>
            @if (s.participants.length) {
              <app-availability-strip [session]="s" />
            } @else {
              <p class="hint">Немає даних для відображення</p>
            }

            @if (s.participants.length) {
              <ion-list [inset]="true" data-testid="participants">
                <ion-list-header><ion-label>Учасники</ion-label></ion-list-header>
                <ion-item-group>
                  @for (p of s.participants; track p.memberKey) {
                    <ion-item>
                      <ion-label class="ion-text-wrap">
                        <h3>{{ p.fullName }}</h3>
                        <p>Важливість голосу: {{ weight(p.roleWeight) }}</p>
                        <p>{{ busyDays(p) }}</p>
                      </ion-label>
                    </ion-item>
                  }
                </ion-item-group>
              </ion-list>
            }

            @if (s.canDelete) {
              <ion-list [inset]="true">
                <ion-item-group>
                  <ion-item
                    [button]="true"
                    [detail]="false"
                    [disabled]="deleting()"
                    (click)="remove()"
                    data-testid="delete"
                  >
                    <ion-label class="center" color="danger">Видалити планування</ion-label>
                  </ion-item>
                </ion-item-group>
              </ion-list>
            }
            <div class="gap"></div>
          }
        }
      }
    </ion-content>
  `,
})
export class PlanningPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly nav = inject(NavController);
  private readonly planning = inject(PlanningService);
  private readonly agenda = inject(AgendaService);
  private readonly auth = inject(AuthService);
  private readonly toasts = inject(Toasts);
  private readonly sheets = inject(ActionSheetController);

  private readonly key = this.route.snapshot.paramMap.get('sessionKey') ?? '';
  /** iOS names where back leads; Material shows the arrow alone. */
  protected readonly backText = inject(Config).get('mode') === 'ios' ? 'Планування' : undefined;
  protected readonly failedText = FAILED_TEXT;
  /** The API refused the read (another kurin's session): the web's «Немає доступу». */
  protected readonly forbidden = signal(false);
  /** Making a calendar event is the web's «Нова подія» right; the API still judges the kurin target. */
  protected readonly canTransfer = canManageAgenda(this.auth.user());
  protected readonly weight = weightLabel;

  protected readonly state = signal<Loaded<PlanningSessionDto>>({ state: 'loading' });
  protected readonly session = computed(() => valueOf(this.state()));
  protected readonly adding = signal(false);
  protected readonly added = signal(false);
  protected readonly deleting = signal(false);

  protected readonly window = computed(() => {
    const s = this.session();
    return s ? windowLabel(s) : '';
  });
  protected readonly length = computed(() => daysLabel(this.session()?.durationDays ?? 0));
  protected readonly people = computed(() =>
    participantsLabel(this.session()?.participants.length ?? 0),
  );
  protected readonly best = computed(() => {
    const s = this.session();
    const days = s ? optimalDays(s) : null;
    return days ? dayRange(days.first, days.last) : null;
  });
  protected readonly busyLine = computed(() => {
    const s = this.session();
    const busy = s ? busyDuringCamp(s) : [];
    return busy.length ? `Зайняті в ці дні: ${busy.join(', ')}` : 'У ці дні вільні всі.';
  });

  constructor() {
    addIcons({ calendarOutline, timeOutline, peopleOutline });
  }

  ngOnInit(): void {
    void this.load();
  }

  protected async refresh(event: Event): Promise<void> {
    await this.load();
    await (event.target as HTMLIonRefresherElement).complete();
  }

  private async load(): Promise<void> {
    try {
      this.state.set({ state: 'ready', value: await this.planning.session(this.key) });
    } catch (error) {
      this.forbidden.set(isForbidden(error));
      if (this.state().state !== 'ready') this.state.set({ state: 'failed' });
    }
  }

  protected busyDays(participant: PlanningParticipantDto): string {
    if (!participant.busyRanges.length) return 'Без зайнятих днів';
    const spans = participant.busyRanges
      .map((range) => spanDays(range))
      .sort((a, b) => a.first.getTime() - b.first.getTime())
      .map((span) => dayRange(span.first, span.last));
    return `Зайняті дні: ${spans.join(', ')}`;
  }

  protected async addToCalendar(): Promise<void> {
    const s = this.session();
    const request = s ? calendarRequest(s) : null;
    if (!request || this.adding()) return;
    this.adding.set(true);
    try {
      await this.agenda.create(request);
      this.added.set(true);
      await this.toasts.show('Додано в календар куреня');
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося додати в календар'), 'danger');
    } finally {
      this.adding.set(false);
    }
  }

  protected async remove(): Promise<void> {
    const s = this.session();
    if (!s || this.deleting() || !(await confirmPlanningDelete(this.sheets, s))) return;
    this.deleting.set(true);
    try {
      await this.planning.delete(s.planningSessionKey);
      await this.toasts.show('Планування видалено');
      await this.nav.navigateBack('/tabs/calendar/planning');
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося видалити.'), 'danger');
    } finally {
      this.deleting.set(false);
    }
  }
}
