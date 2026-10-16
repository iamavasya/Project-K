import { ChangeDetectionStrategy, Component, computed, effect, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from '@openng/optimus-ui/button';
import { DialogModule } from '@openng/optimus-ui/dialog';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { SelectModule } from '@openng/optimus-ui/select';
import { DEFAULT_CURRENCY, money, quarterKey } from '../../functions/dues-format.function';
import { fromQuarterOptions } from '../../functions/from-quarter-options.function';
import { DUES_RECIPIENT_LABELS, DuesRecipient } from '../../models/dues.enums';
import { KurinDuesRateDto, PlastYearDto, QuarterDto, SetKurinDuesRateRequest } from '../../models/group-dues.dto';

/** One row of the dialog: a стаття as it is being typed. */
interface ArticleRow {
  id: number;
  name: string;
  recipient: DuesRecipient;
  amount: number | null;
  reducedAmount: number | null;
}

export const MAX_DUES_ARTICLES = 20;
export const MAX_DUES_ARTICLE_NAME = 60;

/**
 * The Звʼязковий's dialog for the kurin's статті of the quarterly вкладка, from a quarter on: what
 * each стаття is called, where its money goes, and what it costs with and without a пільга.
 * Also offered on a гурток's page, so the first гурток to start can create the rates there.
 */
@Component({
  selector: 'app-kurin-rate-dialog',
  imports: [FormsModule, DialogModule, ButtonModule, SelectModule, InputTextModule],
  templateUrl: './kurin-rate-dialog.html',
  styleUrl: './kurin-rate-dialog.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class KurinRateDialogComponent {
  readonly visible = model(false);
  readonly rates = input<KurinDuesRateDto[]>([]);
  readonly years = input<PlastYearDto[]>([]);
  readonly currentQuarter = input.required<QuarterDto>();
  readonly saving = input(false);
  /** ISO 4217 code of the край's currency, for the totals. */
  readonly currency = input(DEFAULT_CURRENCY);
  readonly save = output<SetKurinDuesRateRequest>();

  readonly fromQuarter = signal('');
  readonly articles = signal<ArticleRow[]>([]);

  readonly maxArticles = MAX_DUES_ARTICLES;
  readonly maxNameLength = MAX_DUES_ARTICLE_NAME;
  readonly recipientOptions = [DuesRecipient.Stanytsia, DuesRecipient.Kurin]
    .map(value => ({ value, label: DUES_RECIPIENT_LABELS[value] }));

  readonly fromQuarterOptions = computed(() => fromQuarterOptions(this.years(), this.currentQuarter()));

  /** What a person pays a quarter in all, without and with a пільга, so the sum is in sight while typing. */
  readonly totals = computed(() => this.articles().reduce(
    (sum, a) => ({
      full: sum.full + (Number(a.amount) || 0),
      reduced: sum.reduced + (Number(a.reducedAmount ?? a.amount) || 0)
    }),
    { full: 0, reduced: 0 }));

  readonly errors = computed(() => {
    const rows = this.articles();
    const errors: string[] = [];
    if (rows.length === 0) {
      errors.push('Додай хоча б одну статтю.');
    }
    if (rows.some(a => a.name.trim().length === 0)) {
      errors.push('Кожна стаття має мати назву.');
    }
    const names = rows.map(a => a.name.trim().toLocaleLowerCase('uk-UA')).filter(n => n.length > 0);
    if (new Set(names).size !== names.length) {
      errors.push('Дві статті не можуть називатися однаково.');
    }
    if (rows.some(a => !isAmount(a.amount) || (a.reducedAmount !== null && !isAmount(a.reducedAmount)))) {
      errors.push('Суми мають бути не менші за нуль.');
    }
    if (rows.some(a => a.reducedAmount !== null && Number(a.reducedAmount) > Number(a.amount))) {
      errors.push('Пільгова сума не може бути вищою за повну.');
    }
    return errors;
  });

  readonly valid = computed(() => this.errors().length === 0);

  readonly money = (amount: number): string => money(amount, this.currency());

  private nextId = 0;

  constructor() {
    effect(() => {
      if (this.visible()) {
        this.reset();
      }
    });
  }

  addArticle(): void {
    if (this.articles().length >= MAX_DUES_ARTICLES) {
      return;
    }
    this.articles.update(rows => [...rows, this.row('', DuesRecipient.Kurin, null, null)]);
  }

  removeArticle(id: number): void {
    this.articles.update(rows => rows.filter(a => a.id !== id));
  }

  setArticle(id: number, change: Partial<Omit<ArticleRow, 'id'>>): void {
    this.articles.update(rows => rows.map(a => (a.id === id ? { ...a, ...change } : a)));
  }

  submit(): void {
    const from = this.fromQuarterOptions().find(o => o.value === this.fromQuarter())?.quarter;
    if (!from || !this.valid()) {
      return;
    }
    this.save.emit({
      fromQuarter: from,
      articles: this.articles().map(a => ({
        name: a.name.trim(),
        recipient: a.recipient,
        amount: Number(a.amount),
        reducedAmount: Number(a.reducedAmount ?? a.amount)
      }))
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
    this.articles.set(now
      ? now.articles.map(a => this.row(a.name, a.recipient, a.amount, a.reducedAmount))
      : [
          this.row('Станиця', DuesRecipient.Stanytsia, null, null),
          this.row('Курінь', DuesRecipient.Kurin, null, null)
        ]);
  }

  private row(name: string, recipient: DuesRecipient, amount: number | null, reducedAmount: number | null): ArticleRow {
    return { id: this.nextId++, name, recipient, amount, reducedAmount };
  }
}

function isAmount(value: number | null): boolean {
  if (value === null || (value as unknown) === '') {
    return false;
  }
  const n = Number(value);
  return Number.isFinite(n) && n >= 0;
}
