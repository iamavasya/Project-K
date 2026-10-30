import { Component, inject, ChangeDetectionStrategy } from '@angular/core';

import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { KraiOption, KurinNumberRange, OnboardingService, WaitlistRegistration } from '../../../services/onboarding-service/onboarding.service';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { CheckboxModule } from '@openng/optimus-ui/checkbox';
import { DatePickerModule } from '@openng/optimus-ui/datepicker';
import { ButtonModule } from '@openng/optimus-ui/button';
import { CardModule } from '@openng/optimus-ui/card';
import { MessageModule } from '@openng/optimus-ui/message';
import { SelectModule } from '@openng/optimus-ui/select';
import { SelectButtonModule } from '@openng/optimus-ui/selectbutton';
import { MessageService } from '@openng/optimus-ui/api';
import { RouterLink } from '@angular/router';
import { PhoneInputComponent } from '../../../../../shared/phone-input/phone-input';

@Component({
  selector: 'app-waitlist-registration',
  imports: [
    ReactiveFormsModule,
    InputTextModule,
    CheckboxModule,
    DatePickerModule,
    PhoneInputComponent,
    ButtonModule,
    CardModule,
    MessageModule,
    SelectModule,
    SelectButtonModule,
    RouterLink
],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <main class="flex justify-center items-center min-h-screen p-4">
      <p-card
        header="Приєднатися до списку очікування"
        subheader="Залиште заявку, щоб отримати доступ до Лілейки"
        [style]="{ width: 'min(100%, 480px)' }"
      >
        @if (submitted) {
          <div class="flex flex-col gap-4">
            <p-message severity="success" styleClass="w-full"
              text="Дякуємо! Заявку надіслано. Ми переглянемо її і напишемо на вказану пошту." />
            <p-message severity="info" styleClass="w-full"
              text="Наші листи часом потрапляють у «Спам» — якщо відповіді не видно, загляньте туди." />
          </div>
        } @else {
        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4">
          <div class="grid grid-cols-2 gap-4">
            <div class="flex flex-col gap-2">
              <label for="firstName">Ім'я</label>
              <input pInputText id="firstName" formControlName="firstName" autocomplete="given-name" />
            </div>
            <div class="flex flex-col gap-2">
              <label for="lastName">Прізвище</label>
              <input pInputText id="lastName" formControlName="lastName" autocomplete="family-name" />
            </div>
          </div>

          <div class="flex flex-col gap-2">
            <label for="email">Email</label>
            <input pInputText id="email" type="email" formControlName="email" autocomplete="email" />
          </div>

          <div class="flex flex-col gap-2">
            <label for="phone">Номер телефону</label>
            <app-phone-input inputId="phone" formControlName="phoneNumber" />
          </div>

          <div class="flex flex-col gap-2">
            <label for="dob">Дата народження</label>
            <p-datePicker id="dob"
              formControlName="dateOfBirth"
              [showIcon]="true"
              dateFormat="dd.mm.yy"
              [maxDate]="maxDate"
             />
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div class="flex flex-col gap-2">
              <label for="stanytsia">Станиця</label>
              <input pInputText id="stanytsia" formControlName="stanytsia" maxlength="120" autocomplete="address-level2" />
            </div>
            @if (kraisUnavailable) {
              <div class="flex flex-col gap-2">
                <label for="regionOrCountry">Край</label>
                <input pInputText id="regionOrCountry" formControlName="regionOrCountry" maxlength="120" autocomplete="country-name" />
              </div>
            } @else {
              <div class="flex flex-col gap-2">
                <label for="krai">Край</label>
                <p-select
                  inputId="krai"
                  formControlName="kraiCode"
                  [options]="krais"
                  optionLabel="name"
                  optionValue="code"
                  [filter]="true"
                  filterBy="name"
                  placeholder="Оберіть край"
                  emptyFilterMessage="Такого краю немає"
                  appendTo="body"
                  styleClass="w-full" />
              </div>
            }
          </div>

          <div class="flex items-center gap-2 mt-2">
            <p-checkbox formControlName="isKurinLeaderCandidate" [binary]="true" inputId="leader" />
            <label for="leader">Я є зв'язковим куреня</label>
          </div>

          @if (!form.get('isKurinLeaderCandidate')?.value) {
            <p-message severity="info" styleClass="w-full"
              text="Наразі ми приймаємо лише курені. Заявки окремих пластунів опрацюємо пізніше." />
          }

          @if (form.get('isKurinLeaderCandidate')?.value) {
            <div class="flex flex-col gap-2">
              <label for="ulad">Улад куреня</label>
              <p-selectbutton
                inputId="ulad"
                formControlName="ulad"
                [options]="uladOptions"
                optionLabel="label"
                optionValue="value"
                [allowEmpty]="false" />
            </div>
          }

          @if (form.get('isKurinLeaderCandidate')?.value && isSenior) {
            <p class="m-0 text-sm text-muted-color">
              Старший курінь може бути в обох уладах — тоді в кожному в нього своє число, а курінь один.
            </p>
            <div class="flex flex-col gap-3">
              <div class="flex items-center gap-3">
                <p-checkbox formControlName="inUsp" [binary]="true" inputId="in-usp" />
                <label for="in-usp" class="w-12">УСП</label>
                <input pInputText id="usp-number" formControlName="uspNumber" inputmode="numeric" placeholder="Число в УСП"
                  aria-label="Число куреня в УСП" class="flex-1" (input)="onDigitsInput($event, 'uspNumber')" />
              </div>
              <div class="flex items-center gap-3">
                <p-checkbox formControlName="inUps" [binary]="true" inputId="in-ups" />
                <label for="in-ups" class="w-12">УПС</label>
                <input pInputText id="ups-number" formControlName="upsNumber" inputmode="numeric" placeholder="Число в УПС"
                  aria-label="Число куреня в УПС" class="flex-1" (input)="onDigitsInput($event, 'upsNumber')" />
              </div>
              @if (form.hasError('noSeniorBranch')) {
                <small class="text-muted-color">Познач хоча б один улад і впиши число куреня в ньому.</small>
              }
            </div>
          }

          @if (form.get('isKurinLeaderCandidate')?.value && !isSenior) {
            <div class="flex items-center gap-2">
              <p-checkbox formControlName="isPreparatoryKurin" [binary]="true" inputId="preparatory" />
              <label for="preparatory">Мій курінь є підготовчим (ще без числа)</label>
            </div>

            @if (!form.get('isPreparatoryKurin')?.value) {
              <div class="flex flex-col gap-2">
                <label for="kurin">Число куреня</label>
                <input
                  pInputText
                  id="kurin"
                  formControlName="claimedKurinNameOrNumber"
                  inputmode="numeric"
                  pattern="[0-9]*"
                  (input)="onKurinNumberInput($event)"
                />
                @if (kurinNumberOutOfRange) {
                  <small class="text-red-500">Число куреня від {{ numberRange.min }} до {{ numberRange.max }}.</small>
                }
              </div>
            } @else {
              <p-message severity="info" styleClass="w-full"
                text="Число куреню можна буде вписати пізніше, у його налаштуваннях." />
            }
          }

          <p-button label="Подати заявку"
            type="submit"
            [disabled]="form.invalid || loading"
            [loading]="loading"
            styleClass="w-full"
           />
          <p class="m-0 text-center text-xs text-muted-color">
            Подаючи заявку, ти погоджуєшся з
            <a routerLink="/privacy" class="text-primary no-underline">політикою конфіденційності</a>.
          </p>
        </form>
        }

        <ng-template pTemplate="footer">
          <div class="text-center text-sm text-muted-color">
            Вже маєте доступ?
            <a routerLink="/login" class="font-semibold text-primary no-underline">Увійти</a>
          </div>
        </ng-template>
      </p-card>
    </main>
  `
})
export class WaitlistRegistrationComponent {
  form: FormGroup;
  loading = false;
  submitted = false;
  maxDate = new Date();
  /** The server says the real range; this is its default until it answers. */
  numberRange: KurinNumberRange = { min: 1, max: 200 };
  krais: KraiOption[] = [];
  /** The list did not load: the applicant names the край in words, as the form used to ask. */
  kraisUnavailable = false;
  readonly uladOptions = [
    { label: 'Юнацтво · УПЮ', value: 'UPYu' },
    { label: 'Старші · УСП, УПС', value: 'senior' }
  ];

  // Only a Зв'язковий may apply for now: the kurin is what gets onboarded, and a person on
  // their own has nowhere to be placed. The box is required, so the button stays disabled
  // and the notice explains why, instead of letting a lone пластун submit and hear nothing.
  private readonly fb = inject(FormBuilder);
  private readonly onboardingService = inject(OnboardingService);
  private readonly messageService = inject(MessageService);

  constructor() {
    this.form = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', Validators.required],
      dateOfBirth: [null, Validators.required],
      stanytsia: ['', [Validators.required, Validators.maxLength(120)]],
      kraiCode: [null as string | null, Validators.required],
      regionOrCountry: [{ value: '', disabled: true }, [Validators.required, Validators.maxLength(120)]],
      isKurinLeaderCandidate: [false, Validators.requiredTrue],
      isPreparatoryKurin: [false],
      claimedKurinNameOrNumber: ['', this.kurinNumberValidators()],
      ulad: ['UPYu' as 'UPYu' | 'senior'],
      inUsp: [false],
      uspNumber: [{ value: '', disabled: true }, this.kurinNumberValidators()],
      inUps: [false],
      upsNumber: [{ value: '', disabled: true }, this.kurinNumberValidators()]
    }, { validators: (control: AbstractControl) => this.seniorBranchChosen(control) });

    // A senior kurin names a number in each улад it is in instead of the one УПЮ number.
    this.form.get('ulad')!.valueChanges.subscribe(() => this.syncNumberFields());
    this.form.get('inUsp')!.valueChanges.subscribe(() => this.syncNumberFields());
    this.form.get('inUps')!.valueChanges.subscribe(() => this.syncNumberFields());

    // A підготовчий курінь has no number yet, so the field steps aside rather than stay required.
    this.form.get('isPreparatoryKurin')!.valueChanges.subscribe(() => this.syncNumberFields());

    this.onboardingService.getKurinNumberRange().subscribe({
      next: range => {
        this.numberRange = range;
        for (const name of ['claimedKurinNameOrNumber', 'uspNumber', 'upsNumber']) {
          const number = this.form.get(name)!;
          number.setValidators(this.kurinNumberValidators());
          number.updateValueAndValidity();
        }
      },
      // The default range stands in; the server still checks the real one.
      error: () => undefined
    });

    this.onboardingService.getKrais().subscribe({
      next: krais => {
        this.krais = krais;
        const krai = this.form.get('kraiCode')!;
        // Most applicants are in the default край, so it comes picked.
        if (!krai.value) {
          krai.setValue(krais.find(k => k.isDefault)?.code ?? null);
        }
      },
      error: () => {
        this.kraisUnavailable = true;
        this.form.get('kraiCode')!.disable();
        this.form.get('regionOrCountry')!.enable();
      }
    });
  }

  get isSenior(): boolean {
    return this.form.get('ulad')!.value === 'senior';
  }

  /** Each number field is on only while it means something, so a hidden one never blocks the form. */
  private syncNumberFields(): void {
    const senior = this.isSenior;
    const preparatory = !!this.form.get('isPreparatoryKurin')!.value;
    this.toggle('claimedKurinNameOrNumber', !senior && !preparatory);
    this.toggle('uspNumber', senior && !!this.form.get('inUsp')!.value);
    this.toggle('upsNumber', senior && !!this.form.get('inUps')!.value);
    this.form.updateValueAndValidity();
  }

  private toggle(name: string, on: boolean): void {
    const control = this.form.get(name)!;
    if (on && control.disabled) {
      control.enable({ emitEvent: false });
    } else if (!on && control.enabled) {
      control.reset('', { emitEvent: false });
      control.disable({ emitEvent: false });
    }
  }

  private seniorBranchChosen(control: AbstractControl): ValidationErrors | null {
    const value = control.value as { ulad?: string; inUsp?: boolean; inUps?: boolean };
    return value.ulad === 'senior' && !value.inUsp && !value.inUps ? { noSeniorBranch: true } : null;
  }

  get kurinNumberOutOfRange(): boolean {
    const number = this.form.get('claimedKurinNameOrNumber')!;
    return number.dirty && (number.hasError('min') || number.hasError('max'));
  }

  private kurinNumberValidators() {
    return [
      Validators.required,
      Validators.pattern(/^\d+$/),
      Validators.min(this.numberRange.min),
      Validators.max(this.numberRange.max)
    ];
  }

  onKurinNumberInput(event: Event): void {
    this.onDigitsInput(event, 'claimedKurinNameOrNumber');
  }

  onDigitsInput(event: Event, controlName: string): void {
    const input = event.target as HTMLInputElement;
    const digitsOnly = input.value.replace(/\D/g, '');

    if (input.value !== digitsOnly) {
      input.value = digitsOnly;
      this.form.get(controlName)?.setValue(digitsOnly, { emitEvent: false });
    }
  }

  /** What the server takes: a senior kurin's numbers per улад, or the one УПЮ number. */
  private registration(): WaitlistRegistration {
    const { ulad, inUsp, uspNumber, inUps, upsNumber, ...rest } = this.form.value;
    if (ulad !== 'senior') {
      return rest;
    }
    return {
      ...rest,
      claimedKurinNameOrNumber: null,
      isPreparatoryKurin: false,
      uspNumber: inUsp && uspNumber ? Number(uspNumber) : null,
      upsNumber: inUps && upsNumber ? Number(upsNumber) : null
    };
  }

  onSubmit() {
    if (this.form.invalid) return;

    this.loading = true;
    this.onboardingService.submitWaitlist(this.registration()).subscribe({
      next: () => {
        this.submitted = true;
        this.loading = false;
        this.messageService.add({
          severity: 'success',
          summary: 'Заявку надіслано',
          detail: 'Відповідь прийде на пошту. Перевірте й теку «Спам».'
        });
      },
      error: (err) => {
        this.messageService.add({
          severity: 'error',
          summary: 'Помилка',
          detail: err.error?.message || 'Не вдалося надіслати заявку'
        });
        this.loading = false;
      }
    });
  }
}
