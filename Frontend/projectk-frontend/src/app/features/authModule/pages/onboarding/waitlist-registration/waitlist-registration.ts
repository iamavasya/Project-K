import { Component, inject, ChangeDetectionStrategy } from '@angular/core';

import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { OnboardingService } from '../../../services/onboarding-service/onboarding.service';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { CheckboxModule } from '@openng/optimus-ui/checkbox';
import { DatePickerModule } from '@openng/optimus-ui/datepicker';
import { InputMaskModule } from '@openng/optimus-ui/inputmask';
import { ButtonModule } from '@openng/optimus-ui/button';
import { CardModule } from '@openng/optimus-ui/card';
import { MessageModule } from '@openng/optimus-ui/message';
import { MessageService } from '@openng/optimus-ui/api';
import { ToastModule } from '@openng/optimus-ui/toast';
import { RouterLink } from '@angular/router';
import { UKRAINIAN_PHONE_MASK, UKRAINIAN_PHONE_PLACEHOLDER } from '../../../../../shared/functions/ukrainian-phone.function';

@Component({
  selector: 'app-waitlist-registration',
  imports: [
    ReactiveFormsModule,
    InputTextModule,
    CheckboxModule,
    DatePickerModule,
    InputMaskModule,
    ButtonModule,
    CardModule,
    MessageModule,
    ToastModule,
    RouterLink
],
  providers: [MessageService],
  changeDetection: ChangeDetectionStrategy.Eager,
  template: `
    <p-toast />
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
            <p-inputMask id="phone"
              formControlName="phoneNumber"
              [mask]="phoneMask"
              [placeholder]="phonePlaceholder"
              autocomplete="tel"
             />
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
            <div class="flex flex-col gap-2">
              <label for="regionOrCountry">Край</label>
              <input pInputText id="regionOrCountry" formControlName="regionOrCountry" maxlength="120" autocomplete="country-name" />
            </div>
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
              <label for="kurin">Число куреня</label>
              <input
                pInputText
                id="kurin"
                formControlName="claimedKurinNameOrNumber"
                inputmode="numeric"
                pattern="[0-9]*"
                (input)="onKurinNumberInput($event)"
              />
            </div>
          }

          <p-button label="Подати заявку"
            type="submit"
            [disabled]="form.invalid || loading"
            [loading]="loading"
            styleClass="w-full"
           />
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
  readonly phoneMask = UKRAINIAN_PHONE_MASK;
  readonly phonePlaceholder = UKRAINIAN_PHONE_PLACEHOLDER;

  // Only a Зв'язковий may apply for now: the kurin is what gets onboarded, and a person on
  // their own has nowhere to be placed. The box is required, so the button stays disabled
  // and the notice explains why, instead of letting a lone пластун submit and hear nothing.
  private fb = inject(FormBuilder);
  private onboardingService = inject(OnboardingService);
  private messageService = inject(MessageService);

  constructor() {
    this.form = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', Validators.required],
      dateOfBirth: [null, Validators.required],
      stanytsia: ['', [Validators.required, Validators.maxLength(120)]],
      regionOrCountry: ['', [Validators.required, Validators.maxLength(120)]],
      isKurinLeaderCandidate: [false, Validators.requiredTrue],
      claimedKurinNameOrNumber: ['', [Validators.required, Validators.pattern(/^[0-9]+$/)]]
    });
  }

  onKurinNumberInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    const digitsOnly = input.value.replace(/\D/g, '');

    if (input.value !== digitsOnly) {
      input.value = digitsOnly;
      this.form.get('claimedKurinNameOrNumber')?.setValue(digitsOnly, { emitEvent: false });
    }
  }

  onSubmit() {
    if (this.form.invalid) return;

    this.loading = true;
    this.onboardingService.submitWaitlist(this.form.value).subscribe({
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
