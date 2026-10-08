import { ChangeDetectionStrategy, Component, computed, inject, input, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CheckboxModule } from '@openng/optimus-ui/checkbox';
import { SelectButtonModule } from '@openng/optimus-ui/selectbutton';
import { MessageService } from '@openng/optimus-ui/api';
import { AgendaService } from '../../services/agenda-service/agenda.service';
import { AgendaAssignmentDto, AgendaItemDto, AgendaItemStatus, AgendaPartDto, COMPLETION_MODE_OPTIONS } from '../../models/agenda';
import { AGENDA_BOARD_COLUMNS, AGENDA_STATUS_META } from '../../models/agenda-status.config';

/**
 * Where a task stands, target by target: who closed it and when, and — for a target done «кожному
 * окремо» — how many are done and, for those who run it, who. Nothing reads «зроблено» without the
 * name of whoever did it.
 */
@Component({
  selector: 'app-agenda-progress',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, FormsModule, CheckboxModule, SelectButtonModule],
  templateUrl: './agenda-progress.html',
  styleUrl: './agenda-progress.css'
})
export class AgendaProgressComponent {
  private readonly agenda = inject(AgendaService);
  private readonly messages = inject(MessageService);

  readonly item = input.required<AgendaItemDto>();
  /** A part moved; the owner reloads the item so every count and column follows. */
  readonly changed = output<void>();

  protected readonly busy = signal<string | null>(null);
  protected readonly statusOptions = AGENDA_BOARD_COLUMNS.map(status => ({ label: AGENDA_STATUS_META[status].label, value: status }));

  /**
   * The viewer's own part when no row below offers it — a youth's share of a task done «кожному
   * окремо», whose row they do not see. Moved through the board's own endpoint, which knows whose it is.
   */
  protected readonly ownPart = computed(() => {
    const item = this.item();
    const offeredBelow = item.assignments.some(a => a.canChangeStatus || a.parts?.some(p => p.canChangeStatus));
    return item.canChangeStatus && !offeredBelow;
  });

  protected modeLabel(assignment: AgendaAssignmentDto): string | null {
    if (assignment.targetType === 'Member' || assignment.completionMode === 'Shared') {
      return null;
    }
    return COMPLETION_MODE_OPTIONS.find(o => o.value === assignment.completionMode)?.label ?? null;
  }

  protected statusLabel(status: AgendaItemStatus): string {
    return AGENDA_STATUS_META[status].label;
  }

  protected moveOwn(status: AgendaItemStatus): void {
    const item = this.item();
    this.run(item.agendaItemKey, this.agenda.changeStatus(item.agendaItemKey, status));
  }

  protected moveTarget(assignment: AgendaAssignmentDto, status: AgendaItemStatus): void {
    this.run(assignment.agendaAssignmentKey,
      this.agenda.changeTargetStatus(this.item().agendaItemKey, assignment.agendaAssignmentKey, status));
  }

  protected movePart(assignment: AgendaAssignmentDto, part: AgendaPartDto, done: boolean): void {
    this.run(part.memberKey,
      this.agenda.changeTargetStatus(this.item().agendaItemKey, assignment.agendaAssignmentKey, done ? 'Done' : 'Todo', part.memberKey));
  }

  private run(key: string, request: ReturnType<AgendaService['changeStatus']>): void {
    if (this.busy()) {
      return;
    }
    this.busy.set(key);
    request.subscribe({
      next: () => {
        this.busy.set(null);
        this.changed.emit();
      },
      error: () => {
        this.busy.set(null);
        this.messages.add({ severity: 'error', summary: 'Не вдалося змінити стан' });
      }
    });
  }
}
