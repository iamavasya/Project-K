import { ChangeDetectionStrategy, Component, inject, input, model, output, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ButtonModule } from '@openng/optimus-ui/button';
import { DialogModule } from '@openng/optimus-ui/dialog';
import { ConfirmationService, MessageService } from '@openng/optimus-ui/api';
import { finalize } from 'rxjs';
import { MemberWarningDto } from '../../../../models/member-warning.dto';
import { MemberWarningLevel } from '../../../../models/enums/member-warning-level.enum';
import { MemberWarningService } from '../../../../services/member-warning-service/member-warning.service';
import { parseUtcDateTime } from '../../../../../../shared/functions/utc-date-time.function';
import { failureDetail } from '../../../../../../shared/functions/failure-detail.function';

export interface WarningStep {
  level: MemberWarningLevel;
  title: string;
  /** The same in the accusative: «дати першу пересторогу». */
  accusative: string;
  term: string;
}

export const WARNING_STEPS: WarningStep[] = [
  { level: MemberWarningLevel.Level1, title: 'Перша пересторога', accusative: 'першу пересторогу', term: 'на 3 місяці' },
  { level: MemberWarningLevel.Level2, title: 'Друга пересторога', accusative: 'другу пересторогу', term: 'на 6 місяців' },
  { level: MemberWarningLevel.Level3, title: 'Третя пересторога', accusative: 'третю пересторогу', term: 'на 12 місяців' },
];

/**
 * Перестороги on the member's card: three in a row, each given or cancelled on its own, at once and
 * after a question. The next one opens only while the one before it stands; only whoever gave one
 * can cancel it.
 */
@Component({
  selector: 'app-member-warnings-dialog',
  imports: [ButtonModule, DialogModule, DatePipe],
  templateUrl: './member-warnings-dialog.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './member-warnings-dialog.css'
})
export class MemberWarningsDialogComponent {
  readonly visible = model(false);
  readonly memberKey = input.required<string>();
  readonly warnings = input<MemberWarningDto[]>([]);
  readonly currentUserKey = input<string | null | undefined>(null);

  /** Something was given or cancelled; the card reloads the member. */
  readonly changed = output<void>();

  readonly steps = WARNING_STEPS;
  readonly busyLevel = signal<MemberWarningLevel | null>(null);

  private readonly warningService = inject(MemberWarningService);
  private readonly confirmation = inject(ConfirmationService);
  private readonly messages = inject(MessageService);

  active(level: MemberWarningLevel): MemberWarningDto | null {
    const now = Date.now();
    return this.warnings().find(warning =>
      warning.level === level
      && !warning.revokedAtUtc
      && (parseUtcDateTime(warning.expiresAtUtc)?.getTime() ?? 0) > now) ?? null;
  }

  expiresAt(level: MemberWarningLevel): Date | null {
    return parseUtcDateTime(this.active(level)?.expiresAtUtc) ?? null;
  }

  /** The one that can be given now: the first not standing, once all before it stand. */
  canGive(level: MemberWarningLevel): boolean {
    const index = this.steps.findIndex(step => step.level === level);
    return !this.active(level) && this.steps.slice(0, index).every(step => !!this.active(step.level));
  }

  canCancel(level: MemberWarningLevel): boolean {
    const warning = this.active(level);
    const me = this.currentUserKey()?.toLowerCase();
    return !!warning && !!me && warning.issuedByUserKey.toLowerCase() === me;
  }

  give(step: WarningStep): void {
    this.confirmation.confirm({
      header: step.title,
      message: `Дати ${step.accusative} ${step.term}?`,
      icon: 'pi pi-exclamation-triangle',
      rejectButtonProps: { label: 'Скасувати', severity: 'secondary', variant: 'text' },
      acceptButtonProps: { label: 'Дати', severity: 'danger' },
      accept: () => this.run(step.level,
        this.warningService.assignWarning(this.memberKey(), { level: step.level }),
        `${step.title} діє.`,
        'Не вдалося дати пересторогу. Спробуй ще раз.')
    });
  }

  cancel(step: WarningStep): void {
    const warning = this.active(step.level);
    if (!warning) {
      return;
    }

    this.confirmation.confirm({
      header: `Скасувати ${step.accusative}`,
      message: `${step.title} перестане діяти зараз, а не в кінці строку.`,
      icon: 'pi pi-exclamation-triangle',
      rejectButtonProps: { label: 'Лишити', severity: 'secondary', variant: 'text' },
      acceptButtonProps: { label: 'Скасувати пересторогу', severity: 'danger' },
      accept: () => this.run(step.level,
        this.warningService.cancelWarning(this.memberKey(), warning.memberWarningKey),
        `${step.title} більше не діє.`,
        'Не вдалося скасувати пересторогу. Спробуй ще раз.')
    });
  }

  private run(level: MemberWarningLevel, request: ReturnType<MemberWarningService['assignWarning']>, done: string, failed: string): void {
    this.busyLevel.set(level);
    request.pipe(finalize(() => this.busyLevel.set(null))).subscribe({
      next: () => {
        this.messages.add({ severity: 'success', summary: done });
        this.changed.emit();
      },
      error: (error: unknown) => this.messages.add({ severity: 'error', summary: failureDetail(error, failed) })
    });
  }
}
