import { ChangeDetectionStrategy, Component, computed, effect, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from '@openng/optimus-ui/button';
import { DialogModule } from '@openng/optimus-ui/dialog';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { SelectModule } from '@openng/optimus-ui/select';
import { quarterKey } from '../../functions/dues-format.function';
import { fromQuarterOptions } from '../../functions/from-quarter-options.function';
import { KurinDuesRateDto, PlastYearDto, QuarterDto, SetKurinDuesRateRequest } from '../../models/group-dues.dto';

/**
 * The станиця and kurin parts of the quarterly вкладка, from a quarter on. The Звʼязковий's dialog,
 * offered on the kurin's page and on a гурток's — so the first гурток to start does not have to go
 * elsewhere to make the numbers exist.
 */
@Component({
  selector: 'app-kurin-rate-dialog',
  imports: [FormsModule, DialogModule, ButtonModule, SelectModule, InputTextModule],
  templateUrl: './kurin-rate-dialog.html',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class KurinRateDialogComponent {
  readonly visible = model(false);
  readonly rates = input<KurinDuesRateDto[]>([]);
  readonly years = input<PlastYearDto[]>([]);
  readonly currentQuarter = input.required<QuarterDto>();
  readonly saving = input(false);
  readonly save = output<SetKurinDuesRateRequest>();

  readonly fromQuarter = signal('');
  readonly stanytsiaFull = signal<number | null>(null);
  readonly stanytsiaReduced = signal<number | null>(null);
  readonly kurinShare = signal<number | null>(null);

  readonly fromQuarterOptions = computed(() => fromQuarterOptions(this.years(), this.currentQuarter()));

  readonly reducedAboveFull = computed(() => {
    const full = this.stanytsiaFull();
    const reduced = this.stanytsiaReduced();
    return full !== null && reduced !== null && Number(reduced) > Number(full);
  });

  readonly valid = computed(() => {
    const numbers = [this.stanytsiaFull(), this.stanytsiaReduced(), this.kurinShare()].map(Number);
    return this.stanytsiaFull() !== null && this.kurinShare() !== null
      && numbers.every(n => Number.isFinite(n) && n >= 0)
      && !this.reducedAboveFull();
  });

  constructor() {
    effect(() => {
      if (this.visible()) {
        this.reset();
      }
    });
  }

  submit(): void {
    const from = this.fromQuarterOptions().find(o => o.value === this.fromQuarter())?.quarter;
    if (!from || !this.valid()) {
      return;
    }
    this.save.emit({
      fromQuarter: from,
      stanytsiaFull: Number(this.stanytsiaFull()),
      stanytsiaReduced: Number(this.stanytsiaReduced() ?? this.stanytsiaFull()),
      kurinShare: Number(this.kurinShare())
    });
  }

  close(): void {
    this.visible.set(false);
  }

  /** Starts from the rate in force now, so a change is an edit of what is, not a blank form. */
  private reset(): void {
    const current = this.currentQuarter();
    const index = (q: QuarterDto) => q.year * 4 + q.number;
    const now = this.rates()
      .filter(r => index(r.fromQuarter) <= index(current))
      .sort((a, b) => index(b.fromQuarter) - index(a.fromQuarter))[0] ?? null;
    this.fromQuarter.set(quarterKey(current));
    this.stanytsiaFull.set(now?.stanytsiaFull ?? null);
    this.stanytsiaReduced.set(now?.stanytsiaReduced ?? now?.stanytsiaFull ?? null);
    this.kurinShare.set(now?.kurinShare ?? null);
  }
}
