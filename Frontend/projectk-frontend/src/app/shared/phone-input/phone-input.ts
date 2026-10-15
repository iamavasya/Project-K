import { ChangeDetectionStrategy, Component, effect, forwardRef, input, signal } from '@angular/core';
import { AbstractControl, ControlValueAccessor, FormsModule, NG_VALIDATORS, NG_VALUE_ACCESSOR, ValidationErrors, Validator } from '@angular/forms';
import { CountryCode } from 'libphonenumber-js';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { SelectModule } from '@openng/optimus-ui/select';
import {
  DEFAULT_PHONE_COUNTRY,
  formatAsYouType,
  isPossiblePhone,
  PHONE_COUNTRIES,
  splitPhone,
  toStoredPhone
} from '../functions/phone.function';

/**
 * A phone number with its country: the country picks the calling code (+420), the box takes the
 * rest as people write it. The form gets the number as it is stored, +420777123456.
 */
@Component({
  selector: 'app-phone-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, SelectModule, InputTextModule],
  providers: [
    { provide: NG_VALUE_ACCESSOR, useExisting: forwardRef(() => PhoneInputComponent), multi: true },
    { provide: NG_VALIDATORS, useExisting: forwardRef(() => PhoneInputComponent), multi: true }
  ],
  template: `
    <div class="phone-input">
      <p-select
        class="phone-input__country"
        [options]="countries"
        optionLabel="label"
        optionValue="code"
        [filter]="true"
        filterBy="label,dialCode"
        [ngModel]="country()"
        (ngModelChange)="setCountry($event)"
        [disabled]="disabled()"
        [ariaLabel]="'Країна номера'"
        appendTo="body">
        <ng-template #selectedItem let-selected>
          @if (selected) { <span>{{ flag(selected.code) }} {{ selected.dialCode }}</span> }
        </ng-template>
        <ng-template #item let-option>
          <span>{{ flag(option.code) }} {{ option.label }} <span class="phone-input__dial">{{ option.dialCode }}</span></span>
        </ng-template>
      </p-select>
      <input
        pInputText
        class="phone-input__number"
        type="tel"
        autocomplete="tel-national"
        [id]="inputId()"
        [ngModel]="national()"
        (ngModelChange)="setNational($event)"
        (blur)="touched()"
        [disabled]="disabled()"
        [attr.aria-invalid]="invalid()" />
    </div>
  `,
  styles: `
    .phone-input { display: flex; gap: 0.5rem; width: 100%; max-width: 24rem; }
    .phone-input__country { flex: 0 0 8.5rem; }
    .phone-input__number { flex: 1 1 auto; min-width: 0; }
    .phone-input__dial { color: var(--p-text-muted-color); margin-left: 0.25rem; }
  `
})
export class PhoneInputComponent implements ControlValueAccessor, Validator {
  /** The country a new number starts in, as the край's (CZ for cz). */
  readonly defaultCountry = input<CountryCode>(DEFAULT_PHONE_COUNTRY);
  readonly inputId = input<string>('phoneNumber');
  readonly invalid = input(false);

  readonly countries = PHONE_COUNTRIES;
  readonly country = signal<CountryCode>(DEFAULT_PHONE_COUNTRY);
  readonly national = signal('');
  readonly disabled = signal(false);

  private written = false;
  private onChange: (value: string) => void = () => undefined;
  private onTouched: () => void = () => undefined;

  constructor() {
    // Until a number is there, the field follows the край it is filled in for.
    effect(() => {
      const fallback = this.defaultCountry();
      if (!this.national()) {
        this.country.set(fallback);
      }
    });
  }

  writeValue(value: string | null | undefined): void {
    const { country, national } = splitPhone(value, this.defaultCountry());
    this.written = !!national;
    this.country.set(country);
    this.national.set(national);
  }

  registerOnChange(fn: (value: string) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(disabled: boolean): void {
    this.disabled.set(disabled);
  }

  validate(control: AbstractControl): ValidationErrors | null {
    return isPossiblePhone(control.value) ? null : { phone: true };
  }

  setCountry(country: CountryCode): void {
    this.country.set(country ?? this.defaultCountry());
    this.emit();
  }

  setNational(value: string): void {
    this.national.set(value ?? '');
    this.written = true;
    this.emit();
  }

  /** Spaces go in once the number is typed, so they never fight the caret. */
  touched(): void {
    this.national.update(national => formatAsYouType(this.country(), national));
    this.onTouched();
  }

  flag(code: string): string {
    return String.fromCodePoint(...[...code].map(letter => 0x1f1e6 + letter.charCodeAt(0) - 65));
  }

  private emit(): void {
    if (this.written) {
      this.onChange(toStoredPhone(this.country(), this.national()));
    }
  }
}
