import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { PASSWORD_RULES } from '../functions/password-rules.function';

@Component({
  selector: 'app-password-rules',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <ul class="password-rules" [attr.id]="listId()" aria-live="polite">
      @for (rule of rules(); track rule.id) {
        <li [class.password-rules__met]="rule.met">
          <i [class]="rule.met ? 'pi pi-check-circle' : 'pi pi-circle'" aria-hidden="true"></i>
          <span>{{ rule.label }}</span>
          <span class="sr-only">{{ rule.met ? '— виконано' : '— ще ні' }}</span>
        </li>
      }
    </ul>
  `,
  styles: `
    .password-rules {
      display: grid;
      gap: 0.25rem;
      margin: 0;
      padding: 0;
      list-style: none;
      color: var(--p-text-muted-color);
      font-size: 0.8125rem;
      line-height: 1.4;
    }

    .password-rules li {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .password-rules i {
      font-size: 0.8125rem;
    }

    .password-rules__met {
      color: var(--p-primary-color);
    }
  `
})
export class PasswordRulesComponent {
  readonly password = input<string | null | undefined>('');
  readonly listId = input<string | null>(null);

  protected readonly rules = computed(() => {
    const password = this.password() ?? '';
    return PASSWORD_RULES.map(rule => ({ id: rule.id, label: rule.label, met: rule.test(password) }));
  });
}
