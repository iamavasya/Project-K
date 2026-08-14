import { ChangeDetectionStrategy, ChangeDetectorRef, Component, computed, effect, inject, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DialogModule } from '@openng/optimus-ui/dialog';
import { ButtonModule } from '@openng/optimus-ui/button';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { TextareaModule } from '@openng/optimus-ui/textarea';
import { SelectButtonModule } from '@openng/optimus-ui/selectbutton';
import { DatePickerModule } from '@openng/optimus-ui/datepicker';
import { MessageService } from '@openng/optimus-ui/api';
import { AgendaService } from '../../services/agenda-service/agenda-service';
import { AgendaAssignSelectComponent } from '../agenda-assign-select/agenda-assign-select';
import { AgendaItemDto, AgendaItemKind, AgendaTargetInput } from '../../models/agenda';

/**
 * Create/edit dialog for an agenda item, per BRANDBOOK §5 (12px, actions bottom-right, Скасувати → primary).
 * Dates are day-only: they are sent as UTC midnight so the calendar and board agree.
 */
@Component({
  selector: 'app-agenda-item-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    FormsModule, DialogModule, ButtonModule, InputTextModule, TextareaModule,
    SelectButtonModule, DatePickerModule, AgendaAssignSelectComponent
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
  readonly saved = output<void>();

  protected readonly kindOptions = [
    { label: 'Подія', value: 'Event' as AgendaItemKind },
    { label: 'Задача', value: 'Task' as AgendaItemKind }
  ];

  protected kind: AgendaItemKind = 'Event';
  protected title = '';
  protected description = '';
  protected startDate: Date | null = null;
  protected endDate: Date | null = null;
  protected readonly targets = signal<AgendaTargetInput[]>([]);
  protected readonly saving = signal(false);
  protected readonly deleting = signal(false);

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
        this.startDate = current.startUtc ? new Date(current.startUtc) : null;
        this.endDate = current.endUtc ? new Date(current.endUtc) : null;
        this.targets.set(current.assignments.map(a => ({ targetType: a.targetType, targetKey: a.targetKey })));
      } else {
        this.resetForm();
      }
      // These are plain fields bound via ngModel; on OnPush the effect's writes need an explicit check.
      this.cdr.markForCheck();
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
      startUtc: this.toUtcMidnight(this.startDate),
      endUtc: this.toUtcMidnight(this.endDate),
      isAllDay: true,
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
    // New items default to the context kind (Event on calendar, Task on board) and start today.
    this.kind = this.defaultKind();
    this.title = '';
    this.description = '';
    this.startDate = new Date();
    this.endDate = null;
    this.targets.set([]);
  }

  /** Day-only selection → UTC midnight ISO string, so the wire value has no local-time drift. */
  private toUtcMidnight(date: Date | null): string | null {
    if (!date) {
      return null;
    }
    return new Date(Date.UTC(date.getFullYear(), date.getMonth(), date.getDate())).toISOString();
  }
}
