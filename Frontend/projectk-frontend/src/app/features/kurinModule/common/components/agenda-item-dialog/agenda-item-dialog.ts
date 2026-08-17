import { ChangeDetectionStrategy, ChangeDetectorRef, Component, computed, effect, inject, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DialogModule } from '@openng/optimus-ui/dialog';
import { ButtonModule } from '@openng/optimus-ui/button';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { TextareaModule } from '@openng/optimus-ui/textarea';
import { SelectButtonModule } from '@openng/optimus-ui/selectbutton';
import { SelectModule } from '@openng/optimus-ui/select';
import { DatePickerModule } from '@openng/optimus-ui/datepicker';
import { ToggleSwitchModule } from '@openng/optimus-ui/toggleswitch';
import { MessageService } from '@openng/optimus-ui/api';
import { AgendaService } from '../../services/agenda-service/agenda-service';
import { AgendaAssignSelectComponent } from '../agenda-assign-select/agenda-assign-select';
import {
  AgendaCategoryDto,
  AgendaItemDto,
  AgendaItemKind,
  AgendaResponsesResponse,
  AgendaRsvpStatus,
  AgendaTargetInput,
  RecurrenceFrequency,
  WEEKDAY_BITS
} from '../../models/agenda';

/**
 * Create/edit dialog for an agenda item, per BRANDBOOK §5 (12px, actions bottom-right, Скасувати → primary).
 * Dates are day-only: they are sent as UTC midnight so the calendar and board agree.
 */
@Component({
  selector: 'app-agenda-item-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule, DialogModule, ButtonModule, InputTextModule, TextareaModule,
    SelectButtonModule, SelectModule, DatePickerModule, ToggleSwitchModule, AgendaAssignSelectComponent
  ],
  templateUrl: './agenda-item-dialog.html',
  styleUrl: './agenda-item-dialog.css'
})
export class AgendaItemDialogComponent {
  private readonly agendaService = inject(AgendaService);
  private readonly messages = inject(MessageService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly visible = model<boolean>(false);
  readonly kurinKey = input.required<string>();
  readonly item = input<AgendaItemDto | null>(null);
  /** Preselected kind when creating: Event from the calendar, Task from the board. */
  readonly defaultKind = input<AgendaItemKind>('Event');
  /** Prefill a fresh item from a calendar slot selection (date/time + all-day-ness). */
  readonly presetStart = input<Date | null>(null);
  readonly presetEnd = input<Date | null>(null);
  readonly presetAllDay = input<boolean>(true);
  readonly saved = output<void>();

  protected readonly kindOptions = [
    { label: 'Подія', value: 'Event' as AgendaItemKind },
    { label: 'Задача', value: 'Task' as AgendaItemKind }
  ];

  protected kind: AgendaItemKind = 'Event';
  protected title = '';
  protected description = '';
  protected allDay = true;
  protected startDate: Date | null = null;
  protected endDate: Date | null = null;
  protected readonly targets = signal<AgendaTargetInput[]>([]);
  protected readonly saving = signal(false);
  protected readonly deleting = signal(false);

  /** Event groups for the picker (events only). */
  protected readonly categories = signal<AgendaCategoryDto[]>([]);
  protected categoryKey: string | null = null;

  /** RSVP picture for an already-saved event; null for tasks and unsaved items. */
  protected readonly rsvp = signal<AgendaResponsesResponse | null>(null);
  protected readonly rsvpSaving = signal(false);
  protected readonly rsvpOptions = [
    { label: 'Йду', value: 'Going' as AgendaRsvpStatus },
    { label: 'Можливо', value: 'Maybe' as AgendaRsvpStatus },
    { label: 'Не йду', value: 'NotGoing' as AgendaRsvpStatus }
  ];

  /** Recurrence rule (v1: freq + interval + weekly weekday mask + end date). */
  protected recurrenceFrequency: RecurrenceFrequency = 'None';
  protected recurrenceInterval = 1;
  protected recurrenceByWeekday = 0;
  protected recurrenceEndDate: Date | null = null;
  protected readonly weekdays = WEEKDAY_BITS;
  protected readonly frequencyOptions = [
    { label: 'Не повторюється', value: 'None' as RecurrenceFrequency },
    { label: 'Щотижня', value: 'Weekly' as RecurrenceFrequency },
    { label: 'Щомісяця', value: 'Monthly' as RecurrenceFrequency },
    { label: 'Щороку', value: 'Yearly' as RecurrenceFrequency }
  ];

  protected readonly canSave = computed(() => this.targets().length > 0);

  constructor() {
    // Populate the form whenever the dialog opens for a specific item (or a fresh create).
    effect(() => {
      if (!this.visible()) {
        return;
      }
      const current = this.item();
      if (current) {
        this.kind = current.kind;
        this.title = current.title;
        this.description = current.description ?? '';
        this.allDay = current.isAllDay;
        this.startDate = current.startUtc ? new Date(current.startUtc) : null;
        this.endDate = current.endUtc ? new Date(current.endUtc) : null;
        this.categoryKey = current.categoryKey ?? null;
        this.recurrenceFrequency = current.recurrenceFrequency ?? 'None';
        this.recurrenceInterval = current.recurrenceInterval || 1;
        this.recurrenceByWeekday = current.recurrenceByWeekday ?? 0;
        this.recurrenceEndDate = current.recurrenceEndUtc ? new Date(current.recurrenceEndUtc) : null;
        this.targets.set(current.assignments.map(a => ({ targetType: a.targetType, targetKey: a.targetKey })));
      } else {
        this.resetForm();
      }

      this.loadCategories();
      this.loadResponses();
      // These are plain fields bound via ngModel; on OnPush the effect's writes need an explicit check.
      this.cdr.markForCheck();
    });
  }

  private loadCategories(): void {
    this.agendaService.getCategories(this.kurinKey()).subscribe(categories => {
      this.categories.set(categories);
      this.cdr.markForCheck();
    });
  }

  /** When a group is picked on a fresh event, inherit its template description and default duration. */
  onCategoryChange(): void {
    const category = this.categories().find(c => c.agendaCategoryKey === this.categoryKey);
    if (!category || this.item()) {
      return;
    }
    if (!this.description.trim() && category.defaultDescription) {
      this.description = category.defaultDescription;
    }
    if (category.defaultDurationMinutes && this.startDate && !this.endDate) {
      this.endDate = new Date(this.startDate.getTime() + category.defaultDurationMinutes * 60_000);
    }
    this.cdr.markForCheck();
  }

  private loadResponses(): void {
    const current = this.item();
    this.rsvp.set(null);
    if (!current || current.kind !== 'Event') {
      return;
    }
    this.agendaService.getResponses(current.agendaItemKey).subscribe({
      next: picture => {
        this.rsvp.set(picture);
        this.cdr.markForCheck();
      },
      error: () => this.rsvp.set(null)
    });
  }

  setRsvp(status: AgendaRsvpStatus): void {
    const current = this.item();
    if (!current) {
      return;
    }
    this.rsvpSaving.set(true);
    this.agendaService.setResponse(current.agendaItemKey, status).subscribe({
      next: picture => {
        this.rsvp.set(picture);
        this.rsvpSaving.set(false);
        this.cdr.markForCheck();
      },
      error: () => {
        this.rsvpSaving.set(false);
        this.messages.add({ severity: 'error', summary: 'Не вдалося зберегти відповідь' });
      }
    });
  }

  save(): void {
    if (!this.canSave() || !this.title.trim()) {
      this.messages.add({ severity: 'warn', summary: 'Заповніть назву та ціль' });
      return;
    }

    this.saving.set(true);
    const payload = {
      kurinKey: this.kurinKey(),
      kind: this.kind,
      title: this.title.trim(),
      description: this.description.trim() || null,
      startUtc: this.toWire(this.startDate),
      endUtc: this.toWire(this.endDate),
      isAllDay: this.allDay,
      agendaCategoryKey: this.kind === 'Event' ? this.categoryKey : null,
      recurrenceFrequency: this.recurrenceFrequency,
      recurrenceInterval: Math.max(1, this.recurrenceInterval || 1),
      recurrenceByWeekday: this.recurrenceFrequency === 'Weekly' ? this.recurrenceByWeekday : 0,
      recurrenceEndUtc: this.recurrenceFrequency !== 'None' ? this.toUtcMidnight(this.recurrenceEndDate) : null,
      recurrenceCount: null,
      targets: this.targets()
    };

    const current = this.item();
    const request$ = current
      ? this.agendaService.update({ ...payload, agendaItemKey: current.agendaItemKey })
      : this.agendaService.create(payload);

    request$.subscribe({
      next: () => {
        this.saving.set(false);
        this.messages.add({ severity: 'success', summary: current ? 'Оновлено' : 'Створено' });
        this.saved.emit();
        this.close();
      },
      error: () => {
        this.saving.set(false);
        this.messages.add({ severity: 'error', summary: 'Не вдалося зберегти' });
      }
    });
  }

  /** Delete a mistakenly created event/task, after a confirm. */
  remove(): void {
    const current = this.item();
    if (!current) {
      return;
    }
    const noun = current.kind === 'Task' ? 'задачу' : 'подію';
    if (!confirm(`Видалити ${noun} «${current.title}»?`)) {
      return;
    }
    this.deleting.set(true);
    this.agendaService.delete(current.agendaItemKey).subscribe({
      next: () => {
        this.deleting.set(false);
        this.messages.add({ severity: 'success', summary: 'Видалено' });
        this.saved.emit();
        this.close();
      },
      error: () => {
        this.deleting.set(false);
        this.messages.add({ severity: 'error', summary: 'Не вдалося видалити' });
      }
    });
  }

  close(): void {
    this.visible.set(false);
  }

  onHide(): void {
    this.resetForm();
  }

  private resetForm(): void {
    // New items default to the context kind (Event on calendar, Task on board). A calendar-slot
    // selection prefills the dates/time and all-day-ness; otherwise it's an all-day item starting today.
    this.kind = this.defaultKind();
    this.title = '';
    this.description = '';
    this.allDay = this.presetAllDay();
    this.startDate = this.presetStart() ?? new Date();
    this.endDate = this.presetEnd();
    this.categoryKey = null;
    this.recurrenceFrequency = 'None';
    this.recurrenceInterval = 1;
    this.recurrenceByWeekday = 0;
    this.recurrenceEndDate = null;
    this.targets.set([]);
    this.rsvp.set(null);
  }

  /** Flip one weekday bit in the weekly recurrence mask. */
  toggleWeekday(bit: number): void {
    this.recurrenceByWeekday ^= bit;
  }

  isWeekdaySelected(bit: number): boolean {
    return (this.recurrenceByWeekday & bit) !== 0;
  }

  /**
   * All-day items are sent as UTC midnight (no local-time drift, every viewer sees the same day); timed
   * items keep their real instant. Recurrence-end always uses the day form.
   */
  private toWire(date: Date | null): string | null {
    if (!date) {
      return null;
    }
    return this.allDay ? this.toUtcMidnight(date) : date.toISOString();
  }

  private toUtcMidnight(date: Date | null): string | null {
    if (!date) {
      return null;
    }
    return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate())).toISOString();
  }
}
