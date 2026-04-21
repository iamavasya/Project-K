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

@Component({
  selector: 'app-waitlist-registration',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, InputTextModule, CheckboxModule, DatePickerModule, InputMaskModule, ButtonModule, CardModule, ToastModule],
  providers: [MessageService],
  template: `
    <p-toast></p-toast>
    <div class="flex justify-center items-center min-h-screen bg-gray-100 p-4">
      <p-card header="Приєднуйтеся до списку очікування" subheader="Зареєструватися для ProjectK ЗБТ!" [style]="{ width: '450px' }">
        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4">
          <div class="grid grid-cols-2 gap-4">
            <div class="flex flex-col gap-2">
              <label for="firstName">Ім'я</label>
              <input pInputText id="firstName" formControlName="firstName" />
            </div>
            <div class="flex flex-col gap-2">
              <label for="lastName">Прізвище</label>
              <input pInputText id="lastName" formControlName="lastName" />
            </div>
          </div>
          
          <div class="flex flex-col gap-2">
            <label for="email">Email</label>
            <input pInputText id="email" type="email" formControlName="email" />
          </div>

          <div class="flex flex-col gap-2">
            <label for="phone">Номер телефону</label>
            <p-inputMask id="phone" formControlName="phoneNumber" mask="+38 (099) 999-99-99" placeholder="+38 (0XX) XXX-XX-XX"></p-inputMask>
          </div>

          <div class="flex flex-col gap-2">
            <label for="dob">Дата народження</label>
            <p-datePicker id="dob" formControlName="dateOfBirth" [showIcon]="true" dateFormat="dd.mm.yy" [maxDate]="maxDate"></p-datePicker>
          </div>

          <div class="flex items-center gap-2 mt-2">
            <p-checkbox formControlName="isKurinLeaderCandidate" [binary]="true" inputId="leader"></p-checkbox>
            <label for="leader">Я є зв'язковим куреня</label>
          </div>
          @if (form.get('isKurinLeaderCandidate')?.value) {
            <div class="flex flex-col gap-2">
              <label for="kurin">Число куреня</label>
              <input pInputText id="kurin" formControlName="claimedKurinNameOrNumber" />
            </div>
          }
          
          <p-button label="Подати заявку" type="submit" [disabled]="form.invalid || loading" [loading]="loading" styleClass="w-full"></p-button>
          
          @if (submitted) {
            <div class="mt-4 p-4 bg-green-100 text-green-700 rounded text-center">
              Дякуємо! Ваш запит був надісланий і очікує на розгляд. Ми зв'яжемося з вами найближчим часом через пошту.
            </div>
          }
        </form>
      </p-card>
    </div>
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
        this.messageService.add({ severity: 'success', summary: 'Submitted', detail: 'Waitlist request received' });
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || 'Submission failed' });
        this.loading = false;
      }
    });
  }
}
