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
  imports: [
    CdkDropList, CdkDrag, ButtonModule, TagModule, ConfirmDialogModule,
    EmptyStateComponent, AgendaItemDialogComponent
  ],
  template: `
    <div class="agenda-page">
      <section class="kurin-tile">
        <div class="agenda-header">
          <h1 class="agenda-title">Задачі</h1>
          @if (canManage()) {
            <p-button label="Нова задача" icon="pi pi-plus" (click)="openCreate()" />
          }
        </div>

        @if (isEmpty()) {
          <app-empty-state
            art="list"
            title="Дошка порожня"
            body="Створи першу задачу — і вона зʼявиться у стовпці «Зробити».">
            @if (canManage()) {
              <p-button label="Нова задача" icon="pi pi-plus" (click)="openCreate()" />
            }
          </app-empty-state>
        } @else {
          <div class="agenda-board" [attr.aria-label]="'Дошка задач'">
            @for (column of columns(); track column.status) {
              <div class="agenda-column">
                <div class="agenda-column__head">
                  <p-tag [severity]="column.severity" [value]="column.label" />
                  <span class="agenda-column__count">{{ column.items.length }}</span>
                </div>
                <div
                  class="agenda-column__list"
                  cdkDropList
                  [id]="column.status"
                  [cdkDropListData]="column.items"
                  [cdkDropListConnectedTo]="columnIds"
                  (cdkDropListDropped)="onDrop($event, column.status)">
                  @for (task of column.items; track task.agendaItemKey) {
                    <div class="agenda-card" cdkDrag [cdkDragDisabled]="!task.canChangeStatus">
                      <div class="agenda-card__title">{{ task.title }}</div>
                      @if (task.assignments.length) {
                        <div class="agenda-card__targets">
                          @for (assignment of task.assignments; track assignment.targetKey) {
                            <span class="agenda-card__chip">{{ assignment.label }}</span>
                          }
                        </div>
                      }
                      @if (task.canEdit) {
                        <div class="agenda-card__actions">
                          <p-button icon="pi pi-pencil" severity="secondary" [rounded]="true" [text]="true" (click)="openEdit(task)" />
                          <p-button icon="pi pi-trash" severity="danger" [rounded]="true" [text]="true" (click)="remove(task)" />
                        </div>
                      }
                    </div>
                  }
                </div>
              </div>
            }
          </div>
        }
      </section>
    </div>

    <app-agenda-item-dialog
      [(visible)]="dialogVisible"
      [kurinKey]="kurinKey()"
      [item]="editing()"
      (saved)="loadData()" />
    <p-confirmDialog />
  `,
  styles: [`
    .agenda-page { margin-inline: auto; padding-block: 2rem; width: min(100% - 2rem, 72rem); }
    .agenda-header { align-items: center; display: flex; gap: 1rem; justify-content: space-between; padding-bottom: 1.25rem; }
    .agenda-title { color: var(--p-text-color); font-size: 1.5rem; font-weight: 800; letter-spacing: -0.02em; margin: 0; }
    .agenda-board { display: grid; gap: 1rem; grid-template-columns: repeat(3, minmax(0, 1fr)); }
    .agenda-column { display: flex; flex-direction: column; gap: 0.75rem; }
    .agenda-column__head { align-items: center; display: flex; gap: 0.5rem; }
    .agenda-column__count { color: var(--p-text-muted-color); font-size: 0.8rem; font-weight: 650; }
    .agenda-column__list {
      background: var(--p-surface-ground);
      border: 1px solid var(--p-content-border-color);
      border-radius: 12px;
      display: flex; flex-direction: column; gap: 0.6rem;
      min-height: 6rem; padding: 0.75rem;
    }
    .agenda-card {
      background: var(--p-content-background);
      border: 1px solid var(--p-content-border-color);
      border-radius: 8px;
      cursor: grab;
      display: flex; flex-direction: column; gap: 0.5rem;
      padding: 0.75rem;
    }
    .agenda-card__title { color: var(--p-text-color); font-weight: 650; }
    .agenda-card__targets { display: flex; flex-wrap: wrap; gap: 0.3rem; }
    .agenda-card__chip {
      background: var(--p-surface-ground);
      border: 1px solid var(--p-content-border-color);
      border-radius: 6px;
      color: var(--p-text-muted-color);
      font-size: 0.72rem; padding: 0.1rem 0.4rem;
    }
    .agenda-card__actions { display: flex; gap: 0.125rem; justify-content: flex-end; }
    .cdk-drag-preview { border-radius: 8px; box-shadow: 0 12px 40px rgba(16, 20, 19, 0.14); }
    .cdk-drag-placeholder { opacity: 0.4; }
    @media (max-width: 768px) { .agenda-board { grid-template-columns: 1fr; } }
  `]
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
