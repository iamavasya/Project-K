import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export interface PasswordRule {
  readonly id: string;
  readonly label: string;
  readonly test: (password: string) => boolean;
}

export const PASSWORD_RULES: readonly PasswordRule[] = [
  { id: 'length', label: 'щонайменше 8 символів', test: password => password.length >= 8 },
  { id: 'upper', label: 'велика латинська літера (A–Z)', test: password => /[A-Z]/.test(password) },
  { id: 'digit', label: 'цифра', test: password => /\d/.test(password) },
  { id: 'symbol', label: 'символ, як-от ! ? # або _', test: password => /[^A-Za-z0-9]/.test(password) }
];

export function passwordMeetsRules(password: string | null | undefined): boolean {
  return PASSWORD_RULES.every(rule => rule.test(password ?? ''));
}

export const passwordRulesValidator: ValidatorFn = (control: AbstractControl): ValidationErrors | null =>
  !control.value || passwordMeetsRules(control.value) ? null : { passwordRules: true };
