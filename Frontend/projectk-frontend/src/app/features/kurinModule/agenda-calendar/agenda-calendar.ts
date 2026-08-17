import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, inject, OnDestroy, OnInit, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';
import { SelectButtonModule } from '@openng/optimus-ui/selectbutton';
import { FullCalendarComponent, FullCalendarModule } from '@fullcalendar/angular';
import { CalendarOptions, DateSelectArg, DatesSetArg, EventClickArg, EventDropArg, EventInput } from '@fullcalendar/core';
import ukLocale from '@fullcalendar/core/locales/uk';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin, { EventResizeDoneArg } from '@fullcalendar/interaction';
import { MessageService } from '@openng/optimus-ui/api';
import { AgendaService } from '../common/services/agenda-service/agenda-service';
import { PermissionService } from '../../authModule/services/permission.service';
import { AgendaItemDialogComponent } from '../common/components/agenda-item-dialog/agenda-item-dialog';
import { AgendaItemDto, UpdateAgendaItemRequest } from '../common/models/agenda';

const DAY_MS = 24 * 60 * 60 * 1000;

/**
 * Month + hourly week/day calendar built on FullCalendar (MIT plugins only: daygrid, timegrid,
 * interaction). Server-expanded recurrence instances render as normal events. FullCalendar's own header
 * is disabled — navigation is driven by native Optimus buttons wired to its API — and the grid is bounded
 * so the timegrid scrolls inside the card instead of stretching the page. Drag/resize writes the new dates
 * back through UpdateAgendaItem.
 */
@Component({
  selector: 'app-agenda-calendar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, ButtonModule, SelectButtonModule, FullCalendarModule, AgendaItemDialogComponent],
  templateUrl: './agenda-calendar.html',
  styleUrl: './agenda-calendar.css'
})
export class AgendaCalendarComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly agendaService = inject(AgendaService);
  private readonly permissionService = inject(PermissionService);
  private readonly messages = inject(MessageService);
  private readonly route = inject(ActivatedRoute);

  private readonly calendar = viewChild(FullCalendarComponent);
  private readonly host = viewChild<ElementRef<HTMLElement>>('calHost');
  private resizeObserver?: ResizeObserver;

  protected readonly kurinKey = signal('');
  protected readonly items = signal<AgendaItemDto[]>([]);
  protected readonly dialogVisible = signal(false);
  protected readonly editing = signal<AgendaItemDto | null>(null);
  protected readonly presetStart = signal<Date | null>(null);
  protected readonly presetEnd = signal<Date | null>(null);
  protected readonly presetAllDay = signal(true);

  /** Title + active view mirrored from FullCalendar so the native toolbar stays in sync. */
  protected readonly viewTitle = signal('');
  protected readonly currentView = signal('dayGridMonth');
  protected readonly viewOptions = [
    { label: 'Місяць', value: 'dayGridMonth' },
    { label: 'Тиждень', value: 'timeGridWeek' },
    { label: 'День', value: 'timeGridDay' }
  ];

  protected readonly calendarOptions = signal<CalendarOptions>({
    plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
    locale: ukLocale,
    initialView: 'dayGridMonth',
    firstDay: 1,
    headerToolbar: false,
    // 'auto' lets FullCalendar render each view at its natural height (a fixed/percentage height collapses
    // the view-harness to 0 in this layout). The .agenda-calendar-host wrapper then caps the height and
    // scrolls, so the timegrid scrolls inside the card instead of stretching the page.
    height: 'auto',
    nowIndicator: true,
    slotMinTime: '06:00:00',
    slotMaxTime: '23:00:00',
    allDaySlot: true,
    editable: true,
    selectable: true,
    selectMirror: true,
    dayMaxEvents: true,
    // Render every event as a solid block (not the default month-view dot) so a category's colour fills
    // the whole event bar in all views. Month view drags by day; the time views drag by time and day.
    eventDisplay: 'block',
    events: [],
    datesSet: (arg: DatesSetArg) => this.onDatesSet(arg),
    eventClick: (arg: EventClickArg) => this.onEventClick(arg),
    select: (arg: DateSelectArg) => this.onSelect(arg),
    eventDrop: (arg: EventDropArg) => this.applyDateChange(arg.event, arg.revert),
    eventResize: (arg: EventResizeDoneArg) => this.applyDateChange(arg.event, arg.revert)
  });

  canManage(): boolean {
    return this.permissionService.canManageAgenda();
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.kurinKey.set(params.get('kurinKey') ?? '');
    });
  }

  ngAfterViewInit(): void {
    // FullCalendar can mount before this flex/overflow host has real dimensions, collapsing the view to
    // zero height (only the day-of-week header shows). A ResizeObserver re-measures it the moment the host
    // gets a size — on first layout and on every later resize — which is the reliable cure for that race.
    const el = this.host()?.nativeElement;
    if (el) {
      this.resizeObserver = new ResizeObserver(() => this.calendar()?.getApi().updateSize());
      this.resizeObserver.observe(el);
    }
  }

  ngOnDestroy(): void {
    this.resizeObserver?.disconnect();
  }

  // ---- Native toolbar → FullCalendar API ----
  private api() {
    return this.calendar()?.getApi();
  }

  prev(): void {
    this.api()?.prev();
  }

  next(): void {
    this.api()?.next();
  }

  today(): void {
    this.api()?.today();
  }

  changeView(view: string): void {
    this.api()?.changeView(view);
  }

  openCreate(): void {
    this.editing.set(null);
    this.presetStart.set(null);
    this.presetEnd.set(null);
    this.presetAllDay.set(true);
    this.dialogVisible.set(true);
  }

  onSaved(): void {
    this.reload();
  }

  private currentRange: { from: string; to: string } | null = null;

  private onDatesSet(arg: DatesSetArg): void {
    this.currentRange = { from: arg.start.toISOString(), to: arg.end.toISOString() };
    this.viewTitle.set(arg.view.title);
    this.currentView.set(arg.view.type);
    this.reload();
    // Deferred re-measure: guards the first paint, where FullCalendar can size itself before the host has
    // layout and collapse the grid body to zero height (only the weekday header would show).
    setTimeout(() => this.calendar()?.getApi().updateSize(), 0);
  }

  private reload(): void {
    if (!this.kurinKey() || !this.currentRange) {
      return;
    }
    this.agendaService.getCalendar(this.kurinKey(), this.currentRange.from, this.currentRange.to)
      .subscribe(items => {
        this.items.set(items);
        this.calendarOptions.update(opts => ({ ...opts, events: items.map(item => this.toEvent(item)) }));
      });
  }

  private toEvent(item: AgendaItemDto): EventInput {
    // All-day events are placed by date only (no time-of-day, no timezone shift); FullCalendar's all-day
    // range is end-exclusive, so the inclusive stored end is pushed one day out. Timed events keep their
    // real UTC instants and render in the viewer's local time.
    const allDay = item.isAllDay;
    const start = allDay ? item.startUtc!.slice(0, 10) : item.startUtc ?? undefined;
    const end = allDay
      ? (item.endUtc ? this.addDays(item.endUtc.slice(0, 10), 1) : undefined)
      : (item.endUtc ?? undefined);

    return {
      id: `${item.agendaItemKey}|${item.startUtc}`,
      title: item.title,
      start,
      end,
      allDay,
      editable: item.canEdit,
      backgroundColor: item.categoryColorHex ?? undefined,
      borderColor: item.categoryColorHex ?? undefined,
      classNames: this.eventClasses(item),
      extendedProps: { item }
    };
  }

  private eventClasses(item: AgendaItemDto): string[] {
    const classes = ['agenda-ev'];
    if (item.isRecurrenceInstance) {
      classes.push('agenda-ev--series');
    }
    if (!item.categoryColorHex) {
      classes.push(item.kind === 'Task' ? `agenda-ev--task agenda-ev--${item.status.toLowerCase()}` : 'agenda-ev--event');
    }
    return classes;
  }

  private onEventClick(arg: EventClickArg): void {
    const item = arg.event.extendedProps['item'] as AgendaItemDto;
    if (item.canEdit) {
      this.editing.set(item);
      this.presetStart.set(null);
      this.dialogVisible.set(true);
    }
  }

  private onSelect(arg: DateSelectArg): void {
    if (!this.canManage()) {
      return;
    }
    this.editing.set(null);
    this.presetAllDay.set(arg.allDay);
    this.presetStart.set(arg.start);
    // An all-day selection's end is exclusive; step back a day for our inclusive model.
    this.presetEnd.set(arg.allDay ? new Date(arg.end.getTime() - DAY_MS) : arg.end);
    this.dialogVisible.set(true);
  }

  /**
   * Common path for drag and resize. A one-off saves the event's new dates directly; a recurring occurrence
   * shifts the whole series (v1) by the same delta the dragged occurrence moved — in whole days for all-day
   * items, in milliseconds for timed ones.
   */
  private applyDateChange(event: EventClickArg['event'], revert: () => void): void {
    const item = event.extendedProps['item'] as AgendaItemDto;
    const allDay = event.allDay;
    let startUtc: string;
    let endUtc: string | null;

    if (item.isRecurrenceInstance) {
      ({ startUtc, endUtc } = this.shiftedSeriesDates(item, event, allDay));
    } else if (allDay) {
      // startStr/endStr are calendar-date strings; persist them as UTC midnight (end is exclusive → −1 day).
      startUtc = this.dateToUtcMidnight(event.startStr.slice(0, 10));
      endUtc = event.endStr ? this.dateToUtcMidnight(this.addDays(event.endStr.slice(0, 10), -1)) : null;
      if (endUtc === startUtc) {
        endUtc = null;
      }
    } else {
      startUtc = event.start ? event.start.toISOString() : item.startUtc!;
      endUtc = event.end ? event.end.toISOString() : null;
    }

    const payload: UpdateAgendaItemRequest = {
      agendaItemKey: item.agendaItemKey,
      kurinKey: item.kurinKey,
      kind: item.kind,
      title: item.title,
      description: item.description,
      startUtc,
      endUtc,
      isAllDay: allDay,
      agendaCategoryKey: item.categoryKey,
      recurrenceFrequency: item.recurrenceFrequency,
      recurrenceInterval: item.recurrenceInterval,
      recurrenceByWeekday: item.recurrenceByWeekday,
      recurrenceEndUtc: item.recurrenceEndUtc,
      recurrenceCount: item.recurrenceCount,
      targets: item.assignments.map(a => ({ targetType: a.targetType, targetKey: a.targetKey }))
    };

    this.agendaService.update(payload).subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: 'Дати оновлено' });
        this.reload();
      },
      error: () => {
        this.messages.add({ severity: 'error', summary: 'Не вдалося оновити дати' });
        revert();
      }
    });
  }

  /**
   * New series start/end when a recurring occurrence is dragged/resized: the series base is shifted by the
   * delta between the occurrence's old and new position (whole days for all-day, real duration for timed).
   */
  private shiftedSeriesDates(item: AgendaItemDto, event: EventClickArg['event'], allDay: boolean): { startUtc: string; endUtc: string | null } {
    if (allDay) {
      const dayDelta = this.daysBetween(item.startUtc!.slice(0, 10), event.startStr.slice(0, 10));
      const seriesStartDay = this.addDays(item.seriesStartUtc!.slice(0, 10), dayDelta);
      // Occurrence span in days (FullCalendar end is exclusive); a single day has no stored end.
      const spanDays = event.endStr ? this.daysBetween(event.startStr.slice(0, 10), event.endStr.slice(0, 10)) : 1;
      const endUtc = spanDays > 1 ? this.dateToUtcMidnight(this.addDays(seriesStartDay, spanDays - 1)) : null;
      return { startUtc: this.dateToUtcMidnight(seriesStartDay), endUtc };
    }

    const startDelta = event.start!.getTime() - new Date(item.startUtc!).getTime();
    const newStart = new Date(new Date(item.seriesStartUtc!).getTime() + startDelta);
    const endUtc = event.end ? new Date(newStart.getTime() + (event.end.getTime() - event.start!.getTime())).toISOString() : null;
    return { startUtc: newStart.toISOString(), endUtc };
  }

  /** Whole-day difference between two 'YYYY-MM-DD' strings. */
  private daysBetween(fromDateStr: string, toDateStr: string): number {
    return Math.round((new Date(`${toDateStr}T00:00:00Z`).getTime() - new Date(`${fromDateStr}T00:00:00Z`).getTime()) / DAY_MS);
  }

  /** 'YYYY-MM-DD' → that calendar day at UTC midnight, as an ISO string. */
  private dateToUtcMidnight(dateStr: string): string {
    return new Date(`${dateStr}T00:00:00Z`).toISOString();
  }

  /** Shift a 'YYYY-MM-DD' string by whole days, staying in the date domain (no timezone drift). */
  private addDays(dateStr: string, days: number): string {
    const d = new Date(`${dateStr}T00:00:00Z`);
    d.setUTCDate(d.getUTCDate() + days);
    return d.toISOString().slice(0, 10);
  }
}
