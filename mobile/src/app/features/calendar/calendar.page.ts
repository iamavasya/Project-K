import { Component, ElementRef, OnDestroy, afterRenderEffect, computed, effect, inject, signal, untracked } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import {
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonIcon,
  IonItem,
  IonItemGroup,
  IonLabel,
  IonList,
  IonListHeader,
  IonNote,
  IonRefresher,
  IonRefresherContent,
  IonSegment,
  IonSegmentButton,
  IonSkeletonText,
  IonTitle,
  IonToggle,
  IonToolbar,
  ViewWillEnter,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { add, chevronBack, chevronForward } from 'ionicons/icons';
import { AuthService } from '../../auth/auth.service';
import { FAILED_TEXT, Loaded } from '../../core/loaded';
import { GlassEffects } from '../../ui/glass';
import { AgendaItemDto } from '../agenda/agenda.models';
import {
  WEEKDAY_HEADERS,
  addDays,
  canManageAgenda,
  dayKey,
  fromDayKey,
  gridWindow,
  itemsByDay,
  longDayLabel,
  monthGrid,
  monthTitle,
  occurrenceId,
  rowTime,
  scheduleOf,
  statusLabel,
  targetsLabel,
} from '../agenda/agenda.labels';
import { AgendaService } from '../agenda/agenda.service';

const SCHEDULES_KEY = 'lil.agenda.showSchedules';
/** How far ahead the list view reads. */
const LIST_DAYS = 60;

/**
 * The kurin's calendar as a phone shows one (iOS Calendar): a month of days with a dot under each
 * day that has something, the chosen day's agenda below it, and a list of what is coming as the
 * other view. «Графіки гуртків» adds every гурток's schedule, as on the web, remembered per device.
 */
@Component({
  selector: 'app-calendar',
  imports: [
    NgTemplateOutlet,
    RouterLink,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonButtons,
    IonButton,
    IonIcon,
    IonContent,
    IonRefresher,
    IonRefresherContent,
    IonSegment,
    IonSegmentButton,
    IonLabel,
    IonList,
    IonListHeader,
    IonItemGroup,
    IonItem,
    IonNote,
    IonToggle,
    IonSkeletonText,
  ],
  styles: `
    .switch {
      padding: 4px 16px 8px;
    }
    .nav {
      display: flex;
      align-items: center;
      padding: 0 8px 0 20px;
    }
    :host-context(.ios) .nav {
      padding-inline-start: 16px;
    }
    .nav h2 {
      flex: 1;
      margin: 0;
      font-size: 20px;
      font-weight: 700;
      color: var(--lk-ink);
    }
    .nav ion-button {
      min-height: 44px;
    }
    .grid {
      margin: 0 12px;
      touch-action: pan-y;
    }
    .week {
      display: grid;
      grid-template-columns: repeat(7, 1fr);
    }
    .weekdays span {
      text-align: center;
      font-size: 12px;
      font-weight: 600;
      color: var(--lk-faint);
      padding: 6px 0 2px;
    }
    .day {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 2px;
      min-height: 48px;
      padding: 4px 0 2px;
      border: 0;
      background: transparent;
      color: var(--lk-ink);
      font: inherit;
      cursor: pointer;
    }
    .num {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 34px;
      height: 34px;
      border-radius: 50%;
      font-size: 17px;
      font-weight: 500;
    }
    .day.other .num {
      color: var(--lk-faint);
    }
    .day.today .num {
      color: var(--lk-primary);
      font-weight: 700;
    }
    .day.selected .num {
      background: var(--lk-ink);
      color: var(--lk-paper);
      font-weight: 700;
    }
    .day.selected.today .num {
      background: var(--lk-primary);
      color: var(--lk-on-primary);
    }
    .dots {
      display: flex;
      gap: 3px;
      height: 6px;
    }
    .dots i {
      width: 6px;
      height: 6px;
      border-radius: 50%;
      background: var(--lk-faint);
    }
    .time {
      width: 52px;
      margin-inline-end: 12px;
      font-size: 13px;
      line-height: 18px;
      color: var(--lk-ink);
      text-align: end;
      font-variant-numeric: tabular-nums;
    }
    .time small {
      display: block;
      color: var(--lk-muted);
      font-size: 13px;
    }
    .bar {
      align-self: stretch;
      width: 4px;
      margin: 10px 12px 10px 0;
      border-radius: 2px;
      background: var(--lk-primary);
    }
    .bar.task {
      background: var(--lk-accent-700);
    }
    .empty {
      margin: 8px 20px 0;
      color: var(--lk-muted);
    }
    :host-context(.ios) .empty {
      margin-inline: 32px;
    }
    .note {
      margin: 6px 20px 24px;
      font-size: 13px;
      color: var(--lk-muted);
    }
    :host-context(.ios) .note {
      margin-inline: 32px;
    }
  `,
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-title>Календар</ion-title>
        @if (canManage()) {
          <ion-buttons slot="end">
            <ion-button
              routerLink="/tabs/calendar/new"
              [queryParams]="{ date: selected() }"
              aria-label="Нова подія"
              data-testid="new-event"
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
        <ion-toolbar><ion-title size="large">Календар</ion-title></ion-toolbar>
      </ion-header>

      @if (!kurinKey) {
        <p class="empty">Ти ще не в курені, тож календаря немає.</p>
      } @else {
        <div class="switch">
          <ion-segment [value]="view()" (ionChange)="setView($event)">
            <ion-segment-button value="month"><ion-label>Місяць</ion-label></ion-segment-button>
            <ion-segment-button value="list"><ion-label>Список</ion-label></ion-segment-button>
          </ion-segment>
        </div>

        @if (view() === 'month') {
          <div class="nav">
            <h2 data-testid="month-title">{{ title() }}</h2>
            <ion-button fill="clear" size="default" (click)="goToday()">Сьогодні</ion-button>
            <ion-button fill="clear" size="default" (click)="shift(-1)" aria-label="Попередній місяць">
              <ion-icon slot="icon-only" name="chevron-back" />
            </ion-button>
            <ion-button fill="clear" size="default" (click)="shift(1)" aria-label="Наступний місяць">
              <ion-icon slot="icon-only" name="chevron-forward" />
            </ion-button>
          </div>
          <div class="grid" (touchstart)="touchStart($event)" (touchend)="touchEnd($event)" data-testid="month">
            <div class="week weekdays">
              @for (name of weekdayHeaders; track name) { <span>{{ name }}</span> }
            </div>
            @for (week of weeks(); track week[0].getTime()) {
              <div class="week">
                @for (date of week; track date.getTime()) {
                  <button
                    type="button"
                    class="day"
                    [class.other]="date.getMonth() !== month().getMonth()"
                    [class.today]="key(date) === today"
                    [class.selected]="key(date) === selected()"
                    [attr.data-day]="key(date)"
                    [attr.aria-label]="dayAria(date)"
                    [attr.aria-pressed]="key(date) === selected()"
                    (click)="select(date)"
                  >
                    <span class="num">{{ date.getDate() }}</span>
                    <span class="dots" aria-hidden="true">
                      @for (dot of dots(date); track $index) { <i [style.background]="dot"></i> }
                    </span>
                  </button>
                }
              </div>
            }
          </div>

          <ion-list [inset]="true" data-testid="day-agenda">
            <ion-list-header><ion-label>{{ selectedLabel() }}</ion-label></ion-list-header>
            <ion-item-group>
              @switch (monthState().state) {
                @case ('loading') {
                  <ion-item><ion-skeleton-text [animated]="true" style="height: 40px" /></ion-item>
                }
                @case ('failed') {
                  <ion-item><ion-label class="ion-text-wrap">{{ failedText }}</ion-label></ion-item>
                }
                @default {
                  @for (item of dayItems(); track rowId(item)) {
                    <ng-container *ngTemplateOutlet="row; context: { $implicit: item, day: selected() }" />
                  } @empty {
                    <ion-item><ion-label class="ion-text-wrap">Цього дня нічого немає.</ion-label></ion-item>
                  }
                }
              }
            </ion-item-group>
          </ion-list>
        } @else {
          @switch (listState().state) {
            @case ('loading') {
              <ion-list [inset]="true">
                <ion-item-group>
                  <ion-item><ion-skeleton-text [animated]="true" style="height: 40px" /></ion-item>
                  <ion-item><ion-skeleton-text [animated]="true" style="height: 40px" /></ion-item>
                </ion-item-group>
              </ion-list>
            }
            @case ('failed') { <p class="empty">{{ failedText }}</p> }
            @default {
              @for (group of upcoming(); track group.day) {
                <ion-list [inset]="true" data-testid="list-day">
                  <ion-list-header><ion-label>{{ group.label }}</ion-label></ion-list-header>
                  <ion-item-group>
                    @for (item of group.items; track rowId(item)) {
                      <ng-container *ngTemplateOutlet="row; context: { $implicit: item, day: group.day }" />
                    }
                  </ion-item-group>
                </ion-list>
              } @empty {
                <p class="empty">Найближчі два місяці нічого не заплановано.</p>
              }
            }
          }
        }

        <ion-list [inset]="true">
          <ion-item-group>
            <ion-item>
              <ion-toggle [checked]="showSchedules()" (ionChange)="setSchedules($event)" justify="space-between" data-testid="schedules">
                Графіки гуртків
              </ion-toggle>
            </ion-item>
          </ion-item-group>
        </ion-list>
        <p class="note">Сходини всіх гуртків куреня поруч із твоїми подіями.</p>
      }

      <ng-template #row let-item let-day="day">
        <ion-item
          [button]="true"
          [detail]="true"
          [routerLink]="['/tabs/calendar/event', item.agendaItemKey]"
          [queryParams]="item.startUtc ? { start: item.startUtc } : {}"
          data-testid="agenda-row"
        >
          <div class="time" slot="start">
            {{ time(item, day).top }}
            @if (time(item, day).bottom) { <small>{{ time(item, day).bottom }}</small> }
          </div>
          <span class="bar" [class.task]="item.kind === 'Task'" [style.background]="item.categoryColorHex" aria-hidden="true"></span>
          <ion-label class="ion-text-wrap">
            <h3>{{ item.title }}</h3>
            @if (subline(item); as text) { <p>{{ text }}</p> }
          </ion-label>
          @if (item.kind === 'Task') {
            <ion-note slot="end">{{ status(item.viewerStatus) }}</ion-note>
          }
        </ion-item>
      </ng-template>
    </ion-content>
  `,
})
export class CalendarPage implements ViewWillEnter, OnDestroy {
  private readonly agenda = inject(AgendaService);
  private readonly auth = inject(AuthService);
  private readonly glass = new GlassEffects(inject<ElementRef<HTMLElement>>(ElementRef).nativeElement);

  protected readonly kurinKey = this.auth.user()?.kurinKey ?? null;
  protected readonly canManage = computed(() => canManageAgenda(this.auth.user()));
  protected readonly today = dayKey(new Date());
  protected readonly weekdayHeaders = WEEKDAY_HEADERS;
  protected readonly failedText = FAILED_TEXT;
  protected readonly key = dayKey;
  protected readonly status = statusLabel;

  protected readonly view = signal<'month' | 'list'>('month');
  protected readonly month = signal(firstOfMonth(new Date()));
  protected readonly selected = signal(this.today);
  protected readonly showSchedules = signal(readSchedules());

  /** Months already read stay, so going back and forth does not flash empty. */
  private readonly months = signal(new Map<string, Loaded<AgendaItemDto[]>>());
  protected readonly listState = signal<Loaded<AgendaItemDto[]>>({ state: 'loading' });

  protected readonly weeks = computed(() => monthGrid(this.month()));
  protected readonly title = computed(() => monthTitle(this.month()));
  private readonly monthKey = computed(() => `${dayKey(this.month())}|${this.showSchedules()}`);
  protected readonly monthState = computed<Loaded<AgendaItemDto[]>>(
    () => this.months().get(this.monthKey()) ?? { state: 'loading' },
  );
  private readonly byDay = computed(() => {
    const state = this.monthState();
    return itemsByDay(state.state === 'ready' ? state.value : []);
  });
  protected readonly dayItems = computed(() => this.byDay().get(this.selected()) ?? []);
  protected readonly selectedLabel = computed(() => capitalize(longDayLabel(fromDayKey(this.selected()))));
  protected readonly upcoming = computed(() => {
    const state = this.listState();
    if (state.state !== 'ready') return [];
    const byDay = itemsByDay(state.value);
    return [...byDay.keys()]
      .filter((day) => day >= this.today)
      .sort((a, b) => a.localeCompare(b))
      .map((day) => ({ day, label: dayHeading(day, this.today), items: byDay.get(day)! }));
  });

  private entered = false;
  private touchX: number | null = null;

  constructor() {
    addIcons({ add, chevronBack, chevronForward });
    effect(() => {
      const view = this.view();
      this.monthKey();
      untracked(() => void (view === 'month' ? this.loadMonth() : this.loadList()));
    });
    afterRenderEffect(() => {
      this.view();
      this.glass.sync();
    });
  }

  /** Back from an item or a form: what changed there shows here. */
  ionViewWillEnter(): void {
    if (this.entered) void this.load();
    this.entered = true;
  }

  ngOnDestroy(): void {
    this.glass.destroy();
  }

  protected async refresh(event: Event): Promise<void> {
    await this.load();
    await (event.target as HTMLIonRefresherElement).complete();
  }

  private load(): Promise<void> {
    return this.view() === 'month' ? this.loadMonth() : this.loadList();
  }

  private async loadMonth(): Promise<void> {
    if (!this.kurinKey) return;
    const cacheKey = this.monthKey();
    const { fromUtc, toUtc } = gridWindow(this.weeks());
    try {
      const items = await this.agenda.calendar(this.kurinKey, fromUtc, toUtc, this.showSchedules());
      this.putMonth(cacheKey, { state: 'ready', value: items });
    } catch {
      if (this.months().get(cacheKey)?.state !== 'ready') this.putMonth(cacheKey, { state: 'failed' });
    }
  }

  private async loadList(): Promise<void> {
    if (!this.kurinKey) return;
    const from = fromDayKey(this.today);
    try {
      const items = await this.agenda.calendar(
        this.kurinKey,
        from.toISOString(),
        addDays(from, LIST_DAYS).toISOString(),
        this.showSchedules(),
      );
      this.listState.set({ state: 'ready', value: items });
    } catch {
      if (this.listState().state !== 'ready') this.listState.set({ state: 'failed' });
    }
  }

  private putMonth(key: string, value: Loaded<AgendaItemDto[]>): void {
    const next = new Map(this.months());
    next.set(key, value);
    this.months.set(next);
  }

  protected setView(event: Event): void {
    const value = (event as CustomEvent<{ value?: string }>).detail.value;
    if (value === 'month' || value === 'list') this.view.set(value);
  }

  protected select(date: Date): void {
    this.selected.set(dayKey(date));
    if (date.getMonth() !== this.month().getMonth()) this.month.set(firstOfMonth(date));
  }

  /** As iOS Calendar: a new month opens on its first day, or on today in this month. */
  protected shift(months: number): void {
    const current = this.month();
    const next = new Date(current.getFullYear(), current.getMonth() + months, 1);
    this.month.set(next);
    const today = fromDayKey(this.today);
    this.selected.set(sameMonth(next, today) ? this.today : dayKey(next));
  }

  protected goToday(): void {
    this.month.set(firstOfMonth(new Date()));
    this.selected.set(this.today);
  }

  protected touchStart(event: TouchEvent): void {
    this.touchX = event.changedTouches[0]?.clientX ?? null;
  }

  /** A sideways swipe across the month turns it. */
  protected touchEnd(event: TouchEvent): void {
    const end = event.changedTouches[0]?.clientX;
    if (this.touchX === null || end === undefined) return;
    const dx = end - this.touchX;
    this.touchX = null;
    if (Math.abs(dx) > 60) this.shift(dx < 0 ? 1 : -1);
  }

  protected setSchedules(event: Event): void {
    const on = (event as CustomEvent<{ checked: boolean }>).detail.checked;
    this.showSchedules.set(on);
    try {
      localStorage.setItem(SCHEDULES_KEY, on ? 'on' : 'off');
    } catch {
      // Blocked storage: the switch still works for this visit.
    }
  }

  /** Up to three dots under a day, in the colour of each item's group. */
  protected dots(date: Date): string[] {
    return (this.byDay().get(dayKey(date)) ?? []).slice(0, 3).map((item) => item.categoryColorHex ?? 'var(--lk-primary)');
  }

  protected dayAria(date: Date): string {
    const count = this.byDay().get(dayKey(date))?.length ?? 0;
    return count ? `${longDayLabel(date)}, записів: ${count}` : longDayLabel(date);
  }

  protected rowId(item: AgendaItemDto): string {
    return occurrenceId(item);
  }

  protected time(item: AgendaItemDto, day: string): { top: string; bottom: string } {
    return rowTime(item, day);
  }

  protected subline(item: AgendaItemDto): string {
    const schedule = scheduleOf(item);
    if (schedule) return `Графік куреня · ${schedule}`;
    return item.location || targetsLabel(item);
  }
}

function firstOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function sameMonth(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth();
}

function capitalize(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1);
}

/** «Сьогодні», «Завтра», else «Субота, 11 жовтня». */
function dayHeading(day: string, today: string): string {
  if (day === today) return 'Сьогодні';
  if (day === dayKey(addDays(fromDayKey(today), 1))) return 'Завтра';
  return capitalize(longDayLabel(fromDayKey(day)));
}

function readSchedules(): boolean {
  try {
    return localStorage.getItem(SCHEDULES_KEY) !== 'off';
  } catch {
    return true;
  }
}
