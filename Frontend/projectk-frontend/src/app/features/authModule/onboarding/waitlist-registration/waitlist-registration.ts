import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { OnboardingService, WaitlistRegistration } from '../../services/onboarding.service';
import { InputTextModule } from 'primeng/inputtext';
import { CheckboxModule } from 'primeng/checkbox';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';

@Component({
  selector: 'app-waitlist-registration',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, InputTextModule, CheckboxModule, ButtonModule, CardModule, ToastModule],
  providers: [MessageService],
  template: `
    <p-toast></p-toast>
    <div class="flex justify-center items-center min-h-screen bg-gray-100 p-4">
      <p-card header="Join the Waitlist" subheader="Register for ProjectK v0.9.0 Beta" [style]="{ width: '400px' }">
        <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4">
          <div class="flex flex-col gap-2">
            <label for="firstName">First Name</label>
            <input pInputText id="firstName" formControlName="firstName" />
          </div>
          <div class="flex flex-col gap-2">
            <label for="lastName">Last Name</label>
            <input pInputText id="lastName" formControlName="lastName" />
          </div>
          <div class="flex flex-col gap-2">
            <label for="email">Email</label>
            <input pInputText id="email" type="email" formControlName="email" />
          </div>
          <div class="flex items-center gap-2">
            <p-checkbox formControlName="isKurinLeaderCandidate" [binary]="true" inputId="leader"></p-checkbox>
            <label for="leader">I am a Kurin Leader Candidate</label>
          </div>
          <div class="flex flex-col gap-2" *ngIf="form.get('isKurinLeaderCandidate')?.value">
            <label for="kurin">Kurin Name or Number</label>
            <input pInputText id="kurin" formControlName="claimedKurinNameOrNumber" />
          </div>
          
          <p-button label="Submit Request" type="submit" [disabled]="form.invalid || loading" [loading]="loading"></p-button>
          
          <div *ngIf="submitted" class="mt-4 p-4 bg-green-100 text-green-700 rounded text-center">
            Thank you! Your request has been submitted and is awaiting review.
          </div>
        </form>
      </p-card>
    </div>
  `
})
export class WaitlistRegistrationComponent {
  form: FormGroup;
  loading: boolean = false;
  submitted: boolean = false;

  constructor(private fb: FormBuilder, private onboardingService: OnboardingService, private messageService: MessageService) {
    this.form = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
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
