import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CdkDrag, CdkDropList, CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { ConfirmationService, MessageService } from 'primeng/api';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { AgendaService } from '../common/services/agenda-service/agenda-service';
import { PermissionService } from '../../authModule/services/permission.service';
import { EmptyStateComponent } from '../../../shared/empty-state/empty-state';
import { AgendaItemDialogComponent } from '../common/components/agenda-item-dialog/agenda-item-dialog';
import { AgendaItemDto, AgendaItemStatus } from '../common/models/agenda';
import { AGENDA_BOARD_COLUMNS, AGENDA_STATUS_META, TagSeverity } from '../common/models/agenda-status.config';

interface BoardColumn {
  status: AgendaItemStatus;
  label: string;
  severity: TagSeverity;
  items: AgendaItemDto[];
}

@Component({
  selector: 'app-agenda-board',
  changeDetection: ChangeDetectionStrategy.OnPush,
  // ConfirmationService is not provided globally; supply it here for the delete confirm dialog.
  providers: [ConfirmationService],
  imports: [
    CdkDropList, CdkDrag, ButtonModule, TagModule, ConfirmDialogModule,
    EmptyStateComponent, AgendaItemDialogComponent
  ],
  templateUrl: './agenda-board.html',
  styleUrl: './agenda-board.css'
})
export class AgendaBoardComponent implements OnInit {
  private readonly agendaService = inject(AgendaService);
  private readonly permissionService = inject(PermissionService);
  private readonly messages = inject(MessageService);
  private readonly confirm = inject(ConfirmationService);
  private readonly route = inject(ActivatedRoute);

  protected readonly kurinKey = signal('');
  protected readonly columns = signal<BoardColumn[]>([]);
  protected readonly dialogVisible = signal(false);
  protected readonly editing = signal<AgendaItemDto | null>(null);

  protected readonly columnIds = AGENDA_BOARD_COLUMNS as string[];
  protected readonly isEmpty = computed(() => this.columns().every(column => column.items.length === 0));

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
    this.agendaService.getBoard(this.kurinKey()).subscribe(items => {
      this.columns.set(this.groupIntoColumns(items));
    });
  }

  onDrop(event: CdkDragDrop<AgendaItemDto[]>, targetStatus: AgendaItemStatus): void {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
      return;
    }

    const task = event.previousContainer.data[event.previousIndex];
    if (!task.canChangeStatus) {
      return;
    }

    transferArrayItem(event.previousContainer.data, event.container.data, event.previousIndex, event.currentIndex);
    const previousStatus = task.status;
    task.status = targetStatus;

    this.agendaService.changeStatus(task.agendaItemKey, targetStatus).subscribe({
      error: () => {
        task.status = previousStatus;
        this.messages.add({ severity: 'error', summary: 'Не вдалося змінити статус' });
        this.loadData();
      }
    });
  }

  openCreate(): void {
    this.editing.set(null);
    this.dialogVisible.set(true);
  }

  openEdit(task: AgendaItemDto): void {
    this.editing.set(task);
    this.dialogVisible.set(true);
  }

  remove(task: AgendaItemDto): void {
    this.confirm.confirm({
      message: `Видалити задачу «${task.title}»?`,
      header: 'Підтвердження',
      icon: 'pi pi-exclamation-triangle',
      acceptLabel: 'Видалити',
      rejectLabel: 'Скасувати',
      accept: () => {
        this.agendaService.delete(task.agendaItemKey).subscribe({
          next: () => this.loadData(),
          error: () => this.messages.add({ severity: 'error', summary: 'Не вдалося видалити' })
        });
      }
    });
  }

  private groupIntoColumns(items: AgendaItemDto[]): BoardColumn[] {
    return AGENDA_BOARD_COLUMNS.map(status => ({
      status,
      label: AGENDA_STATUS_META[status].label,
      severity: AGENDA_STATUS_META[status].severity,
      items: items.filter(item => item.status === status)
    }));
  }
}
