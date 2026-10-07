import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';
import { SkeletonModule } from '@openng/optimus-ui/skeleton';
import { TagModule } from '@openng/optimus-ui/tag';
import { TooltipModule } from '@openng/optimus-ui/tooltip';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state';
import { AgendaItemStatus } from '../../../kurinModule/models/agenda';
import { AGENDA_STATUS_META } from '../../../kurinModule/models/agenda-status.config';
import { dayLabel } from '../../functions/day-label.function';
import { MyTaskDto } from '../../models/me.dto';

export interface TaskStatusChange {
  task: MyTaskDto;
  status: AgendaItemStatus;
}

export interface TaskRow {
  task: MyTaskDto;
  /** The day it is due, if it has one. */
  due: string | null;
  isOverdue: boolean;
}

/**
 * The person's open tasks from every kurin. Done is one tap away for a task they may move in the
 * kurin they act in; the rest are read here and moved on their own board.
 */
@Component({
  selector: 'app-my-tasks-tile',
  imports: [RouterLink, ButtonModule, SkeletonModule, TagModule, TooltipModule, EmptyStateComponent],
  templateUrl: './my-tasks-tile.html',
  styleUrl: './my-tasks-tile.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class MyTasksTileComponent {
  readonly tasks = input<MyTaskDto[]>([]);
  readonly loading = input(false);
  readonly failed = input(false);
  readonly namesKurin = input(false);
  readonly currentKurinKey = input<string | null>(null);
  readonly savingKey = input<string | null>(null);
  readonly changeStatus = output<TaskStatusChange>();

  readonly statusMeta = AGENDA_STATUS_META;
  readonly today = new Date();

  readonly rows = computed<TaskRow[]>(() =>
    this.tasks().map(task => {
      const dueAt = task.endUtc ?? task.startUtc;
      const due = dueAt ? new Date(dueAt) : null;
      return {
        task,
        due: due ? dayLabel(due, this.today) : null,
        isOverdue: due !== null && due.getTime() < this.today.getTime()
      };
    })
  );

  boardLink(): unknown[] | null {
    const key = this.currentKurinKey();
    return key ? ['/tasks', key] : null;
  }

  next(task: MyTaskDto): AgendaItemStatus {
    return task.status === 'Todo' ? 'InProgress' : 'Done';
  }

  nextLabel(task: MyTaskDto): string {
    return task.status === 'Todo' ? 'Почати' : 'Зроблено';
  }
}
