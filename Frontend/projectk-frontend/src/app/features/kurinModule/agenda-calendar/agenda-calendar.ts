import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';
import { FullCalendarModule } from '@fullcalendar/angular';
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
 * interaction). Server-expanded recurrence instances render as normal events; the theme is driven by
 * brandbook CSS variables in the stylesheet. Drag/resize writes the new dates back through UpdateAgendaItem.
 */
@Component({
  selector: 'app-agenda-calendar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, FullCalendarModule, AgendaItemDialogComponent],
  templateUrl: './agenda-calendar.html',
  styleUrl: './agenda-calendar.css'
})
export class AgendaCalendarComponent implements OnInit {
  private readonly agendaService = inject(AgendaService);
  private readonly permissionService = inject(PermissionService);
  private readonly messages = inject(MessageService);
  private readonly route = inject(ActivatedRoute);

  protected readonly kurinKey = signal('');
  protected readonly items = signal<AgendaItemDto[]>([]);
  protected readonly dialogVisible = signal(false);
  protected readonly editing = signal<AgendaItemDto | null>(null);
  protected readonly presetStart = signal<Date | null>(null);
  protected readonly presetEnd = signal<Date | null>(null);
  protected readonly presetAllDay = signal(true);

  protected readonly calendarOptions = signal<CalendarOptions>({
    plugins: [dayGridPlugin, timeGridPlugin, interactionPlugin],
    locale: ukLocale,
    initialView: 'dayGridMonth',
    firstDay: 1,
    height: 'auto',
    headerToolbar: {
      left: 'prev,next today',
      center: 'title',
      right: 'dayGridMonth,timeGridWeek,timeGridDay'
    },
    buttonText: { today: 'Сьогодні', month: 'Місяць', week: 'Тиждень', day: 'День' },
    nowIndicator: true,
    slotMinTime: '06:00:00',
    slotMaxTime: '23:00:00',
    editable: true,
    selectable: true,
    selectMirror: true,
    dayMaxEvents: true,
    events: [],
    datesSet: (arg: DatesSetArg) => this.onDatesSet(arg),
    eventClick: (arg: EventClickArg) => this.onEventClick(arg),
    select: (arg: DateSelectArg) => this.onSelect(arg),
    eventDrop: (arg: EventDropArg) => this.onEventDrop(arg),
    eventResize: (arg: EventResizeDoneArg) => this.onEventResize(arg)
  });

  canManage(): boolean {
    return this.permissionService.canManageAgenda();
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.kurinKey.set(params.get('kurinKey') ?? '');
    });
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

  /** Reload the window FullCalendar is currently showing (kept from the last datesSet). */
  private currentRange: { from: string; to: string } | null = null;

  private onDatesSet(arg: DatesSetArg): void {
    this.currentRange = { from: arg.start.toISOString(), to: arg.end.toISOString() };
    this.reload();
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
    // All-day FullCalendar ranges are end-exclusive; our stored EndUtc is the inclusive last day, so push
    // the display end one day out. Timed events use their real end unchanged.
    const end = item.endUtc
      ? (item.isAllDay ? new Date(new Date(item.endUtc).getTime() + DAY_MS).toISOString() : item.endUtc)
      : undefined;

    const editable = item.canEdit && !item.isRecurrenceInstance;
    return {
      id: `${item.agendaItemKey}|${item.startUtc}`,
      title: item.title,
      start: item.startUtc ?? undefined,
      end,
      allDay: item.isAllDay,
      editable,
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
    // For an all-day range FullCalendar's end is exclusive; step back a day for our inclusive model.
    this.presetEnd.set(arg.allDay ? new Date(arg.end.getTime() - DAY_MS) : arg.end);
    this.dialogVisible.set(true);
  }

  private onEventDrop(arg: EventDropArg): void {
    this.applyDateChange(arg.event, arg.revert);
  }

  private onEventResize(arg: EventResizeDoneArg): void {
    this.applyDateChange(arg.event, arg.revert);
  }

  /** Common path for drag and resize: recurring instances are read-only in v1; others save new dates. */
  private applyDateChange(event: EventClickArg['event'], revert: () => void): void {
    const item = event.extendedProps['item'] as AgendaItemDto;
    if (item.isRecurrenceInstance) {
      this.messages.add({ severity: 'info', summary: 'Повторювану подію змінюйте через редагування серії' });
      revert();
      return;
    }

    const allDay = event.allDay;
    const startUtc = event.start ? event.start.toISOString() : item.startUtc;
    let endUtc: string | null = null;
    if (event.end) {
      // Undo the display shift for all-day ends before persisting.
      endUtc = allDay ? new Date(event.end.getTime() - DAY_MS).toISOString() : event.end.toISOString();
      if (endUtc === startUtc) {
        endUtc = null;
      }
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
}
