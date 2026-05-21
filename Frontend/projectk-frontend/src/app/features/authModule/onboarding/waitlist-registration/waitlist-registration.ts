import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { OnboardingService } from '../../services/onboarding.service';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { DatePickerModule } from 'primeng/datepicker';
import { InputMaskModule } from 'primeng/inputmask';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-waitlist-registration',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    InputTextModule,
    CheckboxModule,
    DatePickerModule,
    InputMaskModule,
    ButtonModule,
    CardModule,
    ToastModule,
    RouterLink
  ],
  providers: [MessageService],
  template: `
    <p-toast></p-toast>
    <main class="flex justify-center items-center min-h-screen bg-gray-100 p-4">
      <p-card
        header="Приєднатися до списку очікування"
        subheader="Залиште заявку, щоб отримати доступ до Project K"
        [style]="{ width: 'min(100%, 480px)' }"
      >
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
            <p-inputMask
              id="phone"
              formControlName="phoneNumber"
              mask="+38 (099) 999-99-99"
              placeholder="+38 (0XX) XXX-XX-XX"
            ></p-inputMask>
          </div>

          <div class="flex flex-col gap-2">
            <label for="dob">Дата народження</label>
            <p-datePicker
              id="dob"
              formControlName="dateOfBirth"
              [showIcon]="true"
              dateFormat="dd.mm.yy"
              [maxDate]="maxDate"
            ></p-datePicker>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div class="flex flex-col gap-2">
              <label for="stanytsia">Станиця</label>
              <input pInputText id="stanytsia" formControlName="stanytsia" maxlength="120" autocomplete="address-level2" />
            </div>
            <div class="flex flex-col gap-2">
              <label for="regionOrCountry">Край / країна</label>
              <input pInputText id="regionOrCountry" formControlName="regionOrCountry" maxlength="120" autocomplete="country-name" />
            </div>
          </div>

          <div class="flex items-center gap-2 mt-2">
            <p-checkbox formControlName="isKurinLeaderCandidate" [binary]="true" inputId="leader"></p-checkbox>
            <label for="leader">Я є зв'язковим куреня</label>
          </div>

          @if (form.get('isKurinLeaderCandidate')?.value) {
            <div class="flex flex-col gap-2">
              <label for="kurin">Число або назва куреня</label>
              <input pInputText id="kurin" formControlName="claimedKurinNameOrNumber" />
            </div>
          }

          <p-button
            label="Подати заявку"
            type="submit"
            [disabled]="form.invalid || loading"
            [loading]="loading"
            styleClass="w-full"
          ></p-button>

          @if (submitted) {
            <div class="mt-4 p-4 bg-green-100 text-green-700 rounded text-center">
              Дякуємо! Вашу заявку надіслано. Ми переглянемо її та зв'яжемося з вами через пошту.
            </div>
          }
        </form>

        <ng-template pTemplate="footer">
          <div class="text-center text-sm text-gray-500">
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
      isKurinLeaderCandidate: [false],
      claimedKurinNameOrNumber: ['']
    });
  }

  onSubmit() {
    if (this.form.invalid) return;

    this.loading = true;
    this.onboardingService.submitWaitlist(this.form.value).subscribe({
      next: () => {
        this.submitted = true;
        this.loading = false;
        this.form.disable();
        this.messageService.add({
          severity: 'success',
          summary: 'Заявку надіслано',
          detail: 'Ваш запит отримано'
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
