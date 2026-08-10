import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import {
  addMonths, differenceInCalendarDays, eachDayOfInterval, endOfMonth, endOfWeek, format,
  isSameMonth, isToday, max as maxDate, min as minDate, startOfMonth, startOfWeek
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
  templateUrl: './agenda-calendar.html',
  styleUrl: './agenda-calendar.css'
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

  /** Base severity: tasks by status; events fall back to secondary and are re-coloured blue via class. */
  tagSeverity(item: AgendaItemDto): TagSeverity {
    return item.kind === 'Task' ? AGENDA_STATUS_META[item.status].severity : 'secondary';
  }

  /** Events add the calendar-only blue class; tasks keep their status severity colour. */
  tagStyleClass(item: AgendaItemDto): string {
    return item.kind === 'Event' ? 'cal-seg__tag cal-seg__tag--event' : 'cal-seg__tag';
  }

  /**
   * All-day items are stored as UTC-midnight; read the calendar date parts and build a local date so the
   * item lands on the same day for every viewer (no drift for far-negative timezone offsets).
   */
  private static localDateOnly(iso: string): Date {
    const [year, month, day] = iso.slice(0, 10).split('-').map(Number);
    return new Date(year, month - 1, day);
  }

  /**
   * Turns the dated items into per-week segments: a multi-day item becomes one bar per week spanning its
   * columns, instead of a chip repeated on each day. Segments are packed into lanes to avoid overlap.
   */
  private buildSegments(items: AgendaItemDto[], weekStart: Date, weekEnd: Date): { segments: WeekSegment[]; laneCount: number } {
    const segments: WeekSegment[] = [];

    for (const item of items) {
      const itemStart = AgendaCalendarComponent.localDateOnly(item.startUtc!);
      const itemEnd = AgendaCalendarComponent.localDateOnly(item.endUtc ?? item.startUtc!);
      if (itemEnd < weekStart || itemStart > weekEnd) {
        continue;
      }

      const segStart = maxDate([itemStart, weekStart]);
      const segEnd = minDate([itemEnd, weekEnd]);

      segments.push({
        item,
        startCol: differenceInCalendarDays(segStart, weekStart) + 1,
        span: differenceInCalendarDays(segEnd, segStart) + 1,
        lane: 0,
        continuesLeft: itemStart < weekStart,
        continuesRight: itemEnd > weekEnd
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
