import { ChangeDetectionStrategy, Component, inject, input, model, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from '@openng/optimus-ui/button';
import { DialogModule } from '@openng/optimus-ui/dialog';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { MembershipCandidateDto, MembershipService } from '../../services/membership-service/membership.service';
import { failureDetail } from '../../../../shared/functions/failure-detail.function';

/**
 * Прийняти в курінь людину, яка вже є в системі, за кодом, який вона віддала.
 *
 * Два кроки навмисно: спершу картка, потім згода. Код вводить людина руками, і помилка в одному
 * символі не має тихо зробити членом когось іншого.
 */
@Component({
  selector: 'app-join-by-code-dialog',
  imports: [FormsModule, DialogModule, InputTextModule, ButtonModule],
  templateUrl: './join-by-code-dialog.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './join-by-code-dialog.css'
})
export class JoinByCodeDialogComponent {
  private readonly memberships = inject(MembershipService);

  readonly kurinKey = input.required<string>();
  readonly visible = model(false);
  readonly joined = output<void>();

  readonly code = signal('');
  readonly candidate = signal<MembershipCandidateDto | null>(null);
  readonly isLooking = signal(false);
  readonly isJoining = signal(false);
  readonly errorMessage = signal<string | null>(null);

  lookUp(): void {
    const code = this.code().trim();
    if (!code) {
      return;
    }

    this.isLooking.set(true);
    this.candidate.set(null);
    this.errorMessage.set(null);

    this.memberships.findCandidate(this.kurinKey(), code).subscribe({
      next: candidate => {
        this.candidate.set(candidate);
        this.isLooking.set(false);
      },
      error: () => {
        this.isLooking.set(false);
        // Та сама відповідь і на неправильний код, і на неіснуючий — так вирішує сервер, і тут
        // ми її не розкладаємо назад на дві.
        this.errorMessage.set('Такого коду ні в кого немає.');
      }
    });
  }

  join(): void {
    const candidate = this.candidate();
    if (!candidate || candidate.alreadyInThisKurin) {
      return;
    }

    this.isJoining.set(true);
    this.memberships.joinByPublicId(this.kurinKey(), candidate.member.publicId).subscribe({
      next: () => {
        this.isJoining.set(false);
        this.close();
        this.joined.emit();
      },
      error: (error: unknown) => {
        this.isJoining.set(false);
        this.errorMessage.set(failureDetail(error, 'Не вдалося прийняти. Спробуй ще раз.'));
      }
    });
  }

  close(): void {
    this.visible.set(false);
    this.code.set('');
    this.candidate.set(null);
    this.errorMessage.set(null);
  }
}
