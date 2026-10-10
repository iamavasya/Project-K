import { Component, computed, inject, input, output, signal } from '@angular/core';
import {
  IonCheckbox,
  IonIcon,
  IonItem,
  IonItemGroup,
  IonLabel,
  IonList,
  IonListHeader,
  IonNote,
  IonSegment,
  IonSegmentButton,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { checkmarkCircle, ellipseOutline, timeOutline } from 'ionicons/icons';
import { apiErrorText } from '../../core/api';
import { Toasts } from '../../core/toast';
import { AgendaAssignmentDto, AgendaItemDto, AgendaItemStatus, AgendaPartDto } from './agenda.models';
import { COMPLETION_MODES, STATUSES, stamp, statusLabel } from './agenda.labels';
import { AgendaService } from './agenda.service';

/**
 * Where a task stands, target by target (the web's agenda-progress): who closed it and when, and for
 * a target done «кожному окремо» how many are done and, for those who run it, who. What can be moved
 * is exactly what the API's `canChangeStatus` flags say.
 */
@Component({
  selector: 'app-agenda-progress',
  imports: [IonList, IonListHeader, IonItemGroup, IonItem, IonLabel, IonNote, IonIcon, IonSegment, IonSegmentButton, IonCheckbox],
  styles: `
    .mark {
      font-size: 22px;
      color: var(--lk-faint);
    }
    .mark[data-status='Done'] {
      color: var(--lk-primary);
    }
    .mark[data-status='InProgress'] {
      color: var(--lk-accent-700);
    }
    .control {
      --inner-padding-end: 16px;
    }
    .control ion-segment {
      margin: 8px 0;
      width: 100%;
    }
    .part {
      --padding-start: 32px;
    }
    .part ion-checkbox {
      width: 100%;
    }
    .who {
      display: block;
      font-size: 13px;
      color: var(--lk-muted);
      font-weight: 400;
    }
  `,
  template: `
    <ion-list [inset]="true" data-testid="progress">
      <ion-list-header><ion-label>Виконання</ion-label></ion-list-header>
      <ion-item-group>
        @if (ownPart()) {
          <ion-item class="control" data-testid="own-part">
            <ion-label class="ion-text-wrap">
              <h3>Твоя частина</h3>
              <ion-segment
                [value]="item().viewerStatus"
                [disabled]="busy() !== null"
                (ionChange)="moveOwn($event)"
              >
                @for (option of statuses; track option.value) {
                  <ion-segment-button [value]="option.value"><ion-label>{{ option.label }}</ion-label></ion-segment-button>
                }
              </ion-segment>
            </ion-label>
          </ion-item>
        }
        @for (assignment of item().assignments; track assignment.agendaAssignmentKey) {
          <ion-item [class.control]="assignment.canChangeStatus" data-testid="target">
            <ion-icon
              slot="start"
              class="mark"
              [attr.data-status]="assignment.status"
              [name]="icon(assignment.status)"
              aria-hidden="true"
            />
            <ion-label class="ion-text-wrap">
              <h3>{{ assignment.label }}</h3>
              @if (modeLabel(assignment); as mode) { <p>{{ mode }}</p> }
              @if (closedBy(assignment); as who) { <p>{{ who }}</p> }
              @if (assignment.canChangeStatus) {
                <ion-segment
                  [value]="assignment.status"
                  [disabled]="busy() !== null"
                  (ionChange)="moveTarget(assignment, $event)"
                >
                  @for (option of statuses; track option.value) {
                    <ion-segment-button [value]="option.value"><ion-label>{{ option.label }}</ion-label></ion-segment-button>
                  }
                </ion-segment>
              }
            </ion-label>
            <ion-note slot="end">
              {{ assignment.peopleCount !== null ? assignment.doneCount + ' з ' + assignment.peopleCount : label(assignment.status) }}
            </ion-note>
          </ion-item>
          @for (part of assignment.parts ?? []; track part.memberKey) {
            <ion-item class="part" data-testid="part">
              <ion-checkbox
                justify="start"
                labelPlacement="end"
                [checked]="part.status === 'Done'"
                [disabled]="!part.canChangeStatus || busy() !== null"
                (ionChange)="movePart(assignment, part, $event)"
              >
                {{ part.name }}
                @if (partNote(part); as note) { <span class="who">{{ note }}</span> }
              </ion-checkbox>
            </ion-item>
          }
        }
      </ion-item-group>
    </ion-list>
  `,
})
export class AgendaProgress {
  private readonly agenda = inject(AgendaService);
  private readonly toasts = inject(Toasts);

  readonly item = input.required<AgendaItemDto>();
  /** A part moved; the owner re-reads the item so every count follows. */
  readonly changed = output<void>();

  protected readonly busy = signal<string | null>(null);
  protected readonly statuses = STATUSES;
  protected readonly label = statusLabel;

  constructor() {
    addIcons({ checkmarkCircle, ellipseOutline, timeOutline });
  }

  /**
   * The viewer's own part when no row below offers it — a youth's share of a task done «кожному
   * окремо», whose row they do not see. Moved through the board's endpoint, which knows whose it is.
   */
  protected readonly ownPart = computed(() => {
    const item = this.item();
    const offeredBelow = item.assignments.some((a) => a.canChangeStatus || a.parts?.some((p) => p.canChangeStatus));
    return item.canChangeStatus && !offeredBelow;
  });

  protected icon(status: AgendaItemStatus): string {
    if (status === 'Done') return 'checkmark-circle';
    return status === 'InProgress' ? 'time-outline' : 'ellipse-outline';
  }

  protected modeLabel(assignment: AgendaAssignmentDto): string | null {
    if (assignment.targetType === 'Member' || assignment.completionMode === 'Shared') return null;
    return COMPLETION_MODES.find((m) => m.value === assignment.completionMode)?.label ?? null;
  }

  /** «Закрито: Богдан Гончар · 10.10, 14:05» — nothing reads «зроблено» without who did it. */
  protected closedBy(assignment: AgendaAssignmentDto): string | null {
    if (assignment.peopleCount !== null || !assignment.statusChangedByName || assignment.status === 'Todo') return null;
    const verb = assignment.status === 'Done' ? 'Закрито' : 'Розпочато';
    return `${verb}: ${assignment.statusChangedByName} · ${stamp(assignment.statusChangedAtUtc)}`;
  }

  protected partNote(part: AgendaPartDto): string | null {
    if (!part.changedByName || part.status === 'Todo') return null;
    const by = part.changedByName !== part.name ? `відмітка: ${part.changedByName} · ` : '';
    return `${by}${stamp(part.changedAtUtc).slice(0, 5)}`;
  }

  protected moveOwn(change: Event): void {
    const status = valueOf(change) as AgendaItemStatus | undefined;
    const item = this.item();
    if (!status || status === item.viewerStatus) return;
    void this.run(item.agendaItemKey, this.agenda.changeStatus(item.agendaItemKey, status), change, item.viewerStatus);
  }

  protected moveTarget(assignment: AgendaAssignmentDto, change: Event): void {
    const status = valueOf(change) as AgendaItemStatus | undefined;
    if (!status || status === assignment.status) return;
    void this.run(
      assignment.agendaAssignmentKey,
      this.agenda.changeTargetStatus(this.item().agendaItemKey, assignment.agendaAssignmentKey, status),
      change,
      assignment.status,
    );
  }

  protected movePart(assignment: AgendaAssignmentDto, part: AgendaPartDto, change: Event): void {
    const done = (change as CustomEvent<{ checked: boolean }>).detail.checked;
    if (done === (part.status === 'Done')) return;
    void this.run(
      part.memberKey,
      this.agenda.changeTargetStatus(this.item().agendaItemKey, assignment.agendaAssignmentKey, done ? 'Done' : 'Todo', part.memberKey),
      change,
      part.status === 'Done',
    );
  }

  /**
   * A refused move puts the control back by hand: its bound value never changed, so Angular would
   * not reset it on its own.
   */
  private async run(key: string, request: Promise<unknown>, change: Event, before: string | boolean): Promise<void> {
    this.busy.set(key);
    try {
      await request;
    } catch (error) {
      const control = change.target as (HTMLIonSegmentElement & HTMLIonCheckboxElement) | null;
      if (control && typeof before === 'boolean') control.checked = before;
      else if (control) control.value = before;
      await this.toasts.show(apiErrorText(error, 'Не вдалося змінити стан. Спробуй ще раз.'), 'danger');
    } finally {
      this.busy.set(null);
      // Re-read either way: a refused move has to put the control back where the server has it.
      this.changed.emit();
    }
  }
}

function valueOf(change: Event): string | undefined {
  const value = (change as CustomEvent<{ value?: string | number }>).detail.value;
  return value === undefined ? undefined : String(value);
}
