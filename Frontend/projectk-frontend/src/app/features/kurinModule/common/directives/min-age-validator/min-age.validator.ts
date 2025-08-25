import { Directive } from '@angular/core';
import { NG_VALIDATORS, Validator, AbstractControl, ValidationErrors } from '@angular/forms';

@Directive({
  selector: '[minAge]',
  standalone: true,
  providers: [
    {
      provide: NG_VALIDATORS,
      useExisting: MinAgeValidatorDirective,
      multi: true
    }
  ]
})
export class MinAgeValidatorDirective implements Validator {
  validate(control: AbstractControl): ValidationErrors | null {
    if (!control.value) return null;

    const birthDate = new Date(control.value);
    const today = new Date();

    const age = today.getFullYear() - birthDate.getFullYear();
    const m = today.getMonth() - birthDate.getMonth();

    const exactAge = m < 0 || (m === 0 && today.getDate() < birthDate.getDate())
      ? age - 1
      : age;

    return exactAge < 6 ? { minAge: { requiredAge: 6, actualAge: exactAge } } : null;
  }
}
