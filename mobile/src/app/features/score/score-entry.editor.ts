import { signal } from '@angular/core';
import { ActionSheetController } from '@ionic/angular';
import { apiErrorText } from '../../core/api';
import { Toasts } from '../../core/toast';
import { points } from './score.format';
import { SCORE_ERRORS, ScoreEntryDto, ScoreEntryTarget, UpsertScoreEntryRequest } from './score.models';
import { ScoreService } from './score.service';

/** What a page opens the «Записати бал» sheet with. */
export interface EntryStart {
  target?: ScoreEntryTarget | null;
  targets?: ScoreEntryTarget[];
  existing?: ScoreEntryDto[];
  entry?: ScoreEntryDto | null;
}

/**
 * The state of the «Записати бал» sheet and its writes, shared by the гурток page and the
 * attendance sheet: create or update, delete after a confirming action sheet, then the page
 * reloads (the totals move on the server, as the web reloads after every write).
 */
export class EntryEditor {
  readonly open = signal(false);
  readonly target = signal<ScoreEntryTarget | null>(null);
  readonly targets = signal<ScoreEntryTarget[]>([]);
  readonly existing = signal<ScoreEntryDto[]>([]);
  readonly entry = signal<ScoreEntryDto | null>(null);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  constructor(
    private readonly scores: ScoreService,
    private readonly sheets: ActionSheetController,
    private readonly toasts: Toasts,
    private readonly kurinKey: () => string,
    private readonly saved: () => Promise<void>,
  ) {}

  start(what: EntryStart): void {
    this.target.set(what.target ?? null);
    this.targets.set(what.targets ?? []);
    this.existing.set(what.existing ?? []);
    this.entry.set(what.entry ?? null);
    this.error.set(null);
    this.open.set(true);
  }

  /** From «Уже є на цій події»: the same sheet, now editing that entry. */
  edit(entry: ScoreEntryDto): void {
    this.entry.set(entry);
    this.error.set(null);
  }

  close(): void {
    this.open.set(false);
  }

  async save(request: UpsertScoreEntryRequest): Promise<void> {
    const editing = this.entry();
    this.saving.set(true);
    this.error.set(null);
    try {
      if (editing) await this.scores.updateEntry(this.kurinKey(), editing.scoreEntryKey, request);
      else await this.scores.createEntry(this.kurinKey(), request);
      this.open.set(false);
      await this.toasts.show(editing ? 'Збережено' : 'Записано');
      await this.saved();
    } catch (error) {
      this.error.set(apiErrorText(error, 'Не вдалося записати. Спробуй ще раз.', SCORE_ERRORS));
    } finally {
      this.saving.set(false);
    }
  }

  async remove(entry: ScoreEntryDto): Promise<void> {
    const sheet = await this.sheets.create({
      header: `${points(entry.points)} · ${entry.itemName ?? entry.reason ?? ''} зникне з балів. Слід у історії змін лишиться.`,
      buttons: [
        { text: 'Видалити', role: 'destructive', data: 'delete' },
        { text: 'Скасувати', role: 'cancel' },
      ],
    });
    await sheet.present();
    const { data } = await sheet.onWillDismiss();
    if (data !== 'delete') return;
    this.saving.set(true);
    try {
      await this.scores.deleteEntry(this.kurinKey(), entry.scoreEntryKey);
      this.open.set(false);
      await this.toasts.show('Видалено');
      await this.saved();
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося видалити. Спробуй ще раз.', SCORE_ERRORS), 'danger');
    } finally {
      this.saving.set(false);
    }
  }
}
