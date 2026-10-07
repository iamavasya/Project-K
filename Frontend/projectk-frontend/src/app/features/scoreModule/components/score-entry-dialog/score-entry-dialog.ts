import { ChangeDetectionStrategy, Component, computed, effect, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from '@openng/optimus-ui/button';
import { DatePickerModule } from '@openng/optimus-ui/datepicker';
import { DialogModule } from '@openng/optimus-ui/dialog';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { SelectModule } from '@openng/optimus-ui/select';
import { SelectButtonModule } from '@openng/optimus-ui/selectbutton';
import { TextareaModule } from '@openng/optimus-ui/textarea';
import { parseDateOnlyString, toDateOnlyString } from '../../../kurinModule/functions/to-date-only-string.function';
import { points } from '../../functions/score-format.function';
import { ScoreEntryDto, ScoreItemDto, UpsertScoreEntryRequest } from '../../models/score.dto';

/** Whom the points go to: a person or a whole гурток. */
export interface ScoreEntryTarget {
  membershipKey: string | null;
  groupKey: string | null;
  name: string;
}

/** The event the points are given at, if any. */
export interface ScoreEntryEvent {
  agendaItemKey: string;
  occurrenceStartUtc: string;
  title: string;
}

/**
 * Points by hand: a position from the kurin's list, or an amount with a reason. Shows what the target
 * already has here so a second суддя sees it; only a repeated position is refused, by the database.
 */
@Component({
  selector: 'app-score-entry-dialog',
  imports: [FormsModule, DialogModule, ButtonModule, SelectModule, SelectButtonModule, DatePickerModule, InputTextModule, TextareaModule],
  templateUrl: './score-entry-dialog.html',
  styleUrl: './score-entry-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ScoreEntryDialogComponent {
  readonly visible = model(false);
  readonly target = input<ScoreEntryTarget | null>(null);
  /** Where a target may be picked: the people of a гурток and the гурток itself. */
  readonly targets = input<ScoreEntryTarget[]>([]);
  readonly event = input<ScoreEntryEvent | null>(null);
  readonly items = input<ScoreItemDto[]>([]);
  /** What the target already has at this event, shown before anything is added. */
  readonly existing = input<ScoreEntryDto[]>([]);
  readonly entry = input<ScoreEntryDto | null>(null);
  readonly saving = input(false);
  readonly errorMessage = input<string | null>(null);
  readonly save = output<UpsertScoreEntryRequest>();
  /** Asked for the entry being edited; the page confirms and deletes, then closes. */
  readonly remove = output<ScoreEntryDto>();

  readonly points = points;
  readonly today = new Date();

  readonly mode = signal<'item' | 'free'>('item');
  readonly targetValue = signal<string | null>(null);
  readonly itemKey = signal<string | null>(null);
  readonly amount = signal<number | null>(null);
  readonly reason = signal('');
  readonly occurredOn = signal<Date | null>(new Date());

  readonly modeOptions = [
    { label: 'З переліку', value: 'item' as const },
    { label: 'Свій бал', value: 'free' as const }
  ];

  readonly hasItems = computed(() => this.items().length > 0);
  readonly canPickTarget = computed(() => !this.target() && !this.entry() && this.targets().length > 0);

  readonly targetOptions = computed(() => this.targets().map(t => ({
    label: t.name,
    value: t.membershipKey ?? `group:${t.groupKey}`
  })));

  readonly itemOptions = computed(() => {
    const taken = new Set(this.existing().map(e => e.scoreItemKey).filter(Boolean));
    return this.items().map(item => ({
      label: `${item.name} · ${points(item.points)}`,
      value: item.scoreItemKey,
      disabled: this.event() !== null && taken.has(item.scoreItemKey) && this.entry()?.scoreItemKey !== item.scoreItemKey
    }));
  });

  /** What else the target has here — the entry being edited is not "else". */
  readonly others = computed(() => {
    const editing = this.entry()?.scoreEntryKey;
    return this.existing().filter(e => e.scoreEntryKey !== editing);
  });

  readonly title = computed(() => this.entry() ? 'Змінити бал' : 'Записати бал');

  readonly targetName = computed(() => {
    const editing = this.entry();
    if (editing) {
      return editing.isForGroup ? `Гурток ${editing.groupName} цілим` : editing.memberName;
    }
    return this.target()?.name ?? null;
  });

  readonly valid = computed(() => {
    if (!this.resolvedTarget()) {
      return false;
    }
    if (!this.occurredOn()) {
      return false;
    }
    if (this.mode() === 'item') {
      return !!this.itemKey();
    }
    const amount = Number(this.amount());
    return Number.isFinite(amount) && amount !== 0 && this.reason().trim().length > 0;
  });

  readonly amountError = computed(() => {
    if (this.mode() !== 'free' || this.amount() === null) {
      return null;
    }
    const amount = Number(this.amount());
    return amount === 0 ? 'Нуль нічого не змінює.' : null;
  });

  constructor() {
    effect(() => {
      if (this.visible()) {
        this.reset();
      }
    });
  }

  submit(): void {
    const target = this.resolvedTarget();
    if (!target || !this.valid() || this.saving()) {
      return;
    }
    const free = this.mode() === 'free';
    this.save.emit({
      membershipKey: target.membershipKey,
      groupKey: target.groupKey,
      scoreItemKey: free ? null : this.itemKey(),
      points: free ? Number(this.amount()) : null,
      reason: free ? this.reason().trim() : null,
      agendaItemKey: this.event()?.agendaItemKey ?? this.entry()?.agendaItemKey ?? null,
      occurrenceStartUtc: this.event()?.occurrenceStartUtc ?? this.entry()?.occurrenceStartUtc ?? null,
      occurredOn: toDateOnlyString(this.occurredOn())!
    });
  }

  close(): void {
    this.visible.set(false);
  }

  private resolvedTarget(): { membershipKey: string | null; groupKey: string | null } | null {
    const editing = this.entry();
    if (editing) {
      return { membershipKey: editing.membershipKey, groupKey: editing.isForGroup ? editing.groupKey : null };
    }
    const fixed = this.target();
    if (fixed) {
      return fixed;
    }
    const value = this.targetValue();
    if (!value) {
      return null;
    }
    return value.startsWith('group:')
      ? { membershipKey: null, groupKey: value.slice('group:'.length) }
      : { membershipKey: value, groupKey: null };
  }

  private reset(): void {
    const editing = this.entry();
    const event = this.event();
    this.mode.set(this.initialMode(editing));
    this.targetValue.set(null);
    this.itemKey.set(editing?.scoreItemKey ?? null);
    this.amount.set(editing && !editing.scoreItemKey ? editing.points : null);
    this.reason.set(editing?.reason ?? '');
    this.occurredOn.set(parseDateOnlyString(editing?.occurredOn ?? event?.occurrenceStartUtc) ?? new Date());
  }

  private initialMode(editing: ScoreEntryDto | null): 'item' | 'free' {
    if (editing) {
      return editing.scoreItemKey ? 'item' : 'free';
    }
    return this.hasItems() ? 'item' : 'free';
  }
}
