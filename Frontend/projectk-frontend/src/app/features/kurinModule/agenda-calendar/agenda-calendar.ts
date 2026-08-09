import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import {
  addMonths, differenceInCalendarDays, eachDayOfInterval, endOfMonth, endOfWeek, format,
  isSameMonth, isToday, max as maxDate, min as minDate, parseISO, startOfDay, startOfMonth, startOfWeek
} from 'date-fns';
import { AgendaService } from '../common/services/agenda-service/agenda-service';
import { PermissionService } from '../../authModule/services/permission.service';
import { AgendaItemDialogComponent } from '../common/components/agenda-item-dialog/agenda-item-dialog';
import { AgendaItemDto } from '../common/models/agenda';
import { AGENDA_STATUS_META, TagSeverity } from '../common/models/agenda-status.config';

interface CalendarDay {
  date: Date;
  dayNumber: string;
  inMonth: boolean;
  isToday: boolean;
  col: number;
}

interface WeekSegment {
  item: AgendaItemDto;
  startCol: number;
  span: number;
  lane: number;
  continuesLeft: boolean;
  continuesRight: boolean;
  multiDay: boolean;
}

interface CalendarWeek {
  days: CalendarDay[];
  segments: WeekSegment[];
  laneCount: number;
}

const UA_MONTHS = [
  'Січень', 'Лютий', 'Березень', 'Квітень', 'Травень', 'Червень',
  'Липень', 'Серпень', 'Вересень', 'Жовтень', 'Листопад', 'Грудень'
];

@Component({
  selector: 'app-agenda-calendar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, TagModule, AgendaItemDialogComponent],
  template: `
    <div class="agenda-page">
      <section class="kurin-tile">
        <div class="agenda-header">
          <h1 class="agenda-title">Календар</h1>
          @if (canManage()) {
            <p-button label="Нова подія" icon="pi pi-plus" (click)="openCreate()" />
          }
        </div>

        <div class="agenda-toolbar">
          <p-button icon="pi pi-chevron-left" severity="secondary" [text]="true" [rounded]="true" (click)="previousMonth()" ariaLabel="Попередній місяць" />
          <span class="agenda-month">{{ monthLabel() }}</span>
          <p-button icon="pi pi-chevron-right" severity="secondary" [text]="true" [rounded]="true" (click)="nextMonth()" ariaLabel="Наступний місяць" />
          <p-button label="Сьогодні" severity="secondary" [text]="true" (click)="goToday()" />
        </div>

        <div class="cal-month" role="grid">
          <div class="cal-weekdays" role="row">
            @for (weekday of weekdays; track weekday) {
              <div class="cal-weekday" role="columnheader">{{ weekday }}</div>
            }
          </div>

          @for (week of weeks(); track $index) {
            <div class="cal-week" [style.--lanes]="week.laneCount" role="row">
              @for (day of week.days; track day.col) {
                <div
                  class="cal-day"
                  [class.cal-day--muted]="!day.inMonth"
                  [class.cal-day--today]="day.isToday"
                  [style.grid-column]="day.col"
                  role="gridcell">
                  <span class="cal-day__num">{{ day.dayNumber }}</span>
                </div>
              }
              @for (seg of week.segments; track seg.item.agendaItemKey + ':' + seg.lane) {
                <button
                  type="button"
                  class="cal-seg"
                  [class.cal-seg--bar]="seg.multiDay"
                  [class.cal-seg--cont-l]="seg.continuesLeft"
                  [class.cal-seg--cont-r]="seg.continuesRight"
                  [style.grid-column]="seg.startCol + ' / span ' + seg.span"
                  [style.grid-row]="seg.lane + 2"
                  (click)="openItem(seg.item)"
                  [attr.aria-label]="seg.item.title">
                  <p-tag [severity]="severityFor(seg.item)" [value]="seg.item.title" styleClass="cal-seg__tag" />
                </button>
              }
            </div>
          }
        </div>
      </section>
    </div>

    <app-agenda-item-dialog
      [visible]="dialogVisible()"
      (visibleChange)="dialogVisible.set($event)"
      [kurinKey]="kurinKey()"
      [item]="editing()"
      (saved)="loadData()" />
  `,
  styles: [`
    .agenda-page { margin-inline: auto; padding-block: 2rem; width: min(100% - 2rem, 72rem); }
    .agenda-header { align-items: center; display: flex; gap: 1rem; justify-content: space-between; padding-bottom: 1rem; }
    .agenda-title { color: var(--p-text-color); font-size: 1.5rem; font-weight: 800; letter-spacing: -0.02em; margin: 0; }
    .agenda-toolbar { align-items: center; display: flex; gap: 0.5rem; padding-bottom: 1rem; }
    .agenda-month { font-weight: 700; min-width: 9rem; text-align: center; }

    .cal-month { border: 1px solid var(--p-content-border-color); border-radius: 12px; overflow: hidden; }
    .cal-weekdays { display: grid; grid-template-columns: repeat(7, minmax(0, 1fr)); }
    .cal-weekday { background: var(--p-surface-ground); color: var(--p-text-muted-color); font-size: 0.72rem; font-weight: 700; letter-spacing: 0.05em; padding: 0.5rem; text-align: center; text-transform: uppercase; }

    .cal-week {
      display: grid;
      grid-template-columns: repeat(7, minmax(0, 1fr));
      grid-template-rows: 1.6rem;
      grid-auto-rows: 1.55rem;
      border-top: 1px solid var(--p-content-border-color);
      padding-bottom: 0.35rem;
    }
    .cal-day {
      grid-row: 1 / -1;
      min-block-size: 6rem;
      border-right: 1px solid var(--p-content-border-color);
      padding: 0.3rem;
    }
    .cal-day:nth-child(7n) { border-right: 0; }
    .cal-day--muted { background: var(--p-surface-ground); }
    .cal-day--muted .cal-day__num { color: var(--p-text-muted-color); }
    .cal-day__num { align-self: flex-start; display: inline-block; font-size: 0.8rem; font-weight: 650; height: 1.5rem; line-height: 1.5rem; min-width: 1.5rem; text-align: center; }
    .cal-day--today .cal-day__num { background: var(--p-primary-color); border-radius: 50%; color: var(--p-primary-contrast-color); }

    .cal-seg { align-self: center; background: none; border: 0; cursor: pointer; margin-inline: 2px; min-width: 0; padding: 0; z-index: 1; }
    :host ::ng-deep .cal-seg .cal-seg__tag { display: flex; justify-content: flex-start; max-width: 100%; overflow: hidden; width: 100%; }
    :host ::ng-deep .cal-seg .cal-seg__tag .p-tag-label { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    /* Continuation edges: flatten the side that runs into an adjacent week. */
    :host ::ng-deep .cal-seg--cont-l .cal-seg__tag { border-bottom-left-radius: 0; border-top-left-radius: 0; }
    :host ::ng-deep .cal-seg--cont-r .cal-seg__tag { border-bottom-right-radius: 0; border-top-right-radius: 0; }

    @media (max-width: 640px) {
      .cal-day { min-block-size: 4.5rem; }
      .cal-weekday { font-size: 0.6rem; padding: 0.3rem 0.1rem; }
    }
  `]
})
export class AgendaCalendarComponent implements OnInit {
  private readonly agendaService = inject(AgendaService);
  private readonly permissionService = inject(PermissionService);
  private readonly route = inject(ActivatedRoute);

  protected readonly weekdays = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Нд'];

  protected readonly kurinKey = signal('');
  protected readonly currentMonth = signal(startOfMonth(new Date()));
  protected readonly items = signal<AgendaItemDto[]>([]);
  protected readonly dialogVisible = signal(false);
  protected readonly editing = signal<AgendaItemDto | null>(null);

  protected readonly monthLabel = computed(() => {
    const month = this.currentMonth();
    return `${UA_MONTHS[month.getMonth()]} ${month.getFullYear()}`;
  });

  protected readonly weeks = computed<CalendarWeek[]>(() => {
    const gridStart = startOfWeek(startOfMonth(this.currentMonth()), { weekStartsOn: 1 });
    const gridEnd = endOfWeek(endOfMonth(this.currentMonth()), { weekStartsOn: 1 });
    const allDays = eachDayOfInterval({ start: gridStart, end: gridEnd });
    const dated = this.items().filter(item => item.startUtc);

    const weeks: CalendarWeek[] = [];
    for (let i = 0; i < allDays.length; i += 7) {
      const days = allDays.slice(i, i + 7);
      const weekStart = days[0];
      const weekEnd = days[days.length - 1];
      weeks.push({
        days: days.map((date, col) => ({
          date,
          dayNumber: format(date, 'd'),
          inMonth: isSameMonth(date, this.currentMonth()),
          isToday: isToday(date),
          col: col + 1
        })),
        ...this.buildSegments(dated, weekStart, weekEnd)
      });
    }
    return weeks;
  });

  canManage(): boolean {
    return this.permissionService.canManageAgenda();
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.kurinKey.set(params.get('kurinKey') ?? '');
      this.loadData();
    });
  }

  loadData(): void {
    if (!this.kurinKey()) {
      return;
    }
    const from = startOfWeek(startOfMonth(this.currentMonth()), { weekStartsOn: 1 });
    const to = endOfWeek(endOfMonth(this.currentMonth()), { weekStartsOn: 1 });
    this.agendaService.getCalendar(this.kurinKey(), from.toISOString(), to.toISOString())
      .subscribe(items => this.items.set(items));
  }

  previousMonth(): void {
    this.currentMonth.set(addMonths(this.currentMonth(), -1));
    this.loadData();
  }

  nextMonth(): void {
    this.currentMonth.set(addMonths(this.currentMonth(), 1));
    this.loadData();
  }

  goToday(): void {
    this.currentMonth.set(startOfMonth(new Date()));
    this.loadData();
  }

  openCreate(): void {
    this.editing.set(null);
    this.dialogVisible.set(true);
  }

  openItem(item: AgendaItemDto): void {
    if (item.canEdit) {
      this.editing.set(item);
      this.dialogVisible.set(true);
    }
  }

  /** Events read as a distinct dark `contrast` tag; tasks keep their status colour. No new palette colour. */
  severityFor(item: AgendaItemDto): TagSeverity {
    return item.kind === 'Task' ? AGENDA_STATUS_META[item.status].severity : 'contrast';
  }

  /**
   * Turns the dated items into per-week segments: a multi-day item becomes one bar per week spanning its
   * columns, instead of a chip repeated on each day. Segments are packed into lanes to avoid overlap.
   */
  private buildSegments(items: AgendaItemDto[], weekStart: Date, weekEnd: Date): { segments: WeekSegment[]; laneCount: number } {
    const segments: WeekSegment[] = [];

    for (const item of items) {
      const itemStart = startOfDay(parseISO(item.startUtc!));
      const itemEnd = startOfDay(parseISO(item.endUtc ?? item.startUtc!));
      if (itemEnd < weekStart || itemStart > weekEnd) {
        continue;
      }

      const segStart = maxDate([itemStart, weekStart]);
      const segEnd = minDate([itemEnd, weekEnd]);
      const startCol = differenceInCalendarDays(segStart, weekStart) + 1;
      const span = differenceInCalendarDays(segEnd, segStart) + 1;
      const continuesLeft = itemStart < weekStart;
      const continuesRight = itemEnd > weekEnd;

      segments.push({
        item,
        startCol,
        span,
        lane: 0,
        continuesLeft,
        continuesRight,
        multiDay: differenceInCalendarDays(itemEnd, itemStart) >= 1 || continuesLeft || continuesRight
      });
    }

    // Greedy lane packing: earliest start first, longest first on ties, then first free lane.
    segments.sort((a, b) => a.startCol - b.startCol || b.span - a.span);
    const laneEndCol: number[] = [];
    for (const seg of segments) {
      let lane = 0;
      while (lane < laneEndCol.length && laneEndCol[lane] >= seg.startCol) {
        lane++;
      }
      seg.lane = lane;
      laneEndCol[lane] = seg.startCol + seg.span - 1;
    }

    return { segments, laneCount: laneEndCol.length };
  }
}
