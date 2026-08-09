import { ChangeDetectionStrategy, Component, computed, effect, inject, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { SelectButtonModule } from 'primeng/selectbutton';
import { DatePickerModule } from 'primeng/datepicker';
import { MessageService } from 'primeng/api';
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
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="visible.set($event)"
      [modal]="true"
      [style]="{ width: '34rem' }"
      [draggable]="false"
      [header]="item() ? 'Редагувати' : 'Нове призначення'"
      (onHide)="onHide()">
      <div class="agenda-form">
        <div class="agenda-field">
          <label class="agenda-label">Тип</label>
          <p-selectButton [options]="kindOptions" [(ngModel)]="kind" optionLabel="label" optionValue="value" [allowEmpty]="false" />
        </div>

        <div class="agenda-field">
          <label class="agenda-label" for="agenda-title">Назва</label>
          <input id="agenda-title" pInputText [(ngModel)]="title" maxlength="200" placeholder="Про що це?" />
        </div>

        <div class="agenda-field">
          <label class="agenda-label" for="agenda-desc">Опис</label>
          <textarea id="agenda-desc" pTextarea [(ngModel)]="description" rows="3" maxlength="2000" placeholder="Деталі (необовʼязково)"></textarea>
        </div>

        <div class="agenda-dates">
          <div class="agenda-field">
            <label class="agenda-label" for="agenda-start">Початок</label>
            <p-datePicker id="agenda-start" [(ngModel)]="startDate" dateFormat="dd.mm.yy" [showIcon]="true" appendTo="body" />
          </div>
          <div class="agenda-field">
            <label class="agenda-label" for="agenda-end">Кінець</label>
            <p-datePicker id="agenda-end" [(ngModel)]="endDate" dateFormat="dd.mm.yy" [showIcon]="true" appendTo="body" />
          </div>
        </div>

        <div class="agenda-field">
          <label class="agenda-label">Призначити для</label>
          <app-agenda-assign-select [kurinKey]="kurinKey()" [targets]="targets()" (targetsChange)="targets.set($event)" />
        </div>
      </div>

      <ng-template pTemplate="footer">
        <p-button label="Скасувати" severity="secondary" variant="text" (click)="close()" />
        <p-button label="Зберегти" icon="pi pi-check" [disabled]="!canSave() || saving()" (click)="save()" />
      </ng-template>
    </p-dialog>
  `,
  styles: [`
    .agenda-form { display: flex; flex-direction: column; gap: 1rem; padding-top: 0.25rem; }
    .agenda-field { display: flex; flex-direction: column; gap: 0.4rem; }
    .agenda-label { color: var(--p-text-muted-color); font-size: 0.8rem; font-weight: 650; }
    .agenda-dates { display: grid; gap: 1rem; grid-template-columns: 1fr 1fr; }
    .agenda-form input, .agenda-form textarea { width: 100%; }
    @media (max-width: 640px) { .agenda-dates { grid-template-columns: 1fr; } }
  `]
})
export class AgendaItemDialogComponent {
  private readonly agendaService = inject(AgendaService);
  private readonly messages = inject(MessageService);

  readonly visible = model<boolean>(false);
  readonly kurinKey = input.required<string>();
  readonly item = input<AgendaItemDto | null>(null);
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

  close(): void {
    this.visible.set(false);
  }

  onHide(): void {
    this.resetForm();
  }

  private resetForm(): void {
    this.kind = 'Event';
    this.title = '';
    this.description = '';
    this.startDate = null;
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
