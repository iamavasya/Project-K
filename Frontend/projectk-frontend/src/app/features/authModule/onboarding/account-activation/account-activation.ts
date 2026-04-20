import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { OnboardingService, InvitationValidationResponse } from '../../services/onboarding.service';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { MessageService } from 'primeng/api';
import { ToastModule } from 'primeng/toast';

@Component({
  selector: 'app-account-activation',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, InputTextModule, PasswordModule, ButtonModule, CardModule, ToastModule],
  providers: [MessageService],
  template: `
    <p-toast></p-toast>
    <div class="flex justify-center items-center min-h-screen bg-gray-100 p-4">
      <p-card header="Account Activation" [style]="{ width: '400px' }">
        <div *ngIf="loading && !validationData" class="text-center p-4">
          <i class="pi pi-spin pi-spinner text-4xl"></i>
          <p>Validating invitation...</p>
        </div>

        <div *ngIf="!loading && !validationData?.isValid" class="text-center p-4">
          <i class="pi pi-exclamation-triangle text-red-500 text-4xl mb-4"></i>
          <h3 class="text-xl font-bold">Invalid or Expired Invitation</h3>
          <p class="text-gray-600 mb-4">The activation link you followed is no longer valid.</p>
          <p-button label="Back to Login" (onClick)="goToLogin()"></p-button>
        </div>

        <div *ngIf="validationData?.isValid">
          <div class="mb-6 p-4 bg-blue-50 rounded-lg">
            <p class="font-bold text-blue-800">Welcome, {{ validationData?.firstName }}!</p>
            <p class="text-sm text-blue-600">Please set a password to activate your account for {{ validationData?.email }}.</p>
          </div>

          <form [formGroup]="form" (ngSubmit)="onSubmit()" class="flex flex-col gap-4">
            <div class="flex flex-col gap-2">
              <label for="password">New Password</label>
              <p-password id="password" formControlName="password" [feedback]="true" [toggleMask]="true" styleClass="w-full" inputStyleClass="w-full"></p-password>
            </div>
            <div class="flex flex-col gap-2">
              <label for="confirmPassword">Confirm Password</label>
              <p-password id="confirmPassword" formControlName="confirmPassword" [feedback]="false" [toggleMask]="true" styleClass="w-full" inputStyleClass="w-full"></p-password>
              <small *ngIf="form.errors?.['mismatch'] && form.get('confirmPassword')?.touched" class="p-error text-red-500">
                Passwords do not match
              </small>
            </div>
            
            <p-button label="Activate Account" type="submit" [disabled]="form.invalid || submitting" [loading]="submitting" styleClass="w-full"></p-button>
          </form>
        </div>

        <div *ngIf="activated" class="mt-4 p-4 bg-green-100 text-green-700 rounded text-center">
          Success! Your account is active. Redirecting you...
        </div>
      </p-card>
    </div>
  `
})
export class AccountActivationComponent implements OnInit {
  token: string | null = null;
  loading: boolean = true;
  submitting: boolean = false;
  activated: boolean = false;
  validationData: InvitationValidationResponse | null = null;
  form: FormGroup;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fb: FormBuilder,
    private onboardingService: OnboardingService,
    private messageService: MessageService
  ) {
    this.form = this.fb.group({
      password: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });
  }

  ngOnInit() {
    this.token = this.route.snapshot.paramMap.get('token');
    if (this.token) {
      this.validateToken();
    } else {
      this.loading = false;
    }
  }

  validateToken() {
    this.onboardingService.validateInvitation(this.token!).subscribe({
      next: (data) => {
        this.validationData = data;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      }
    });
  }

  passwordMatchValidator(g: FormGroup) {
    return g.get('password')?.value === g.get('confirmPassword')?.value ? null : { mismatch: true };
  }

  onSubmit() {
    if (this.form.invalid) return;

    this.submitting = true;
    const payload = {
      token: this.token,
      password: this.form.value.password
    };

    this.onboardingService.activateAccount(payload).subscribe({
      next: () => {
        this.activated = true;
        this.submitting = false;
        this.messageService.add({ severity: 'success', summary: 'Activated', detail: 'Welcome to ProjectK!' });
        setTimeout(() => this.router.navigate(['/login']), 2000);
      },
      error: (err) => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: err.error?.message || 'Activation failed' });
        this.submitting = false;
      }
    });
  }

  goToLogin() {
    this.router.navigate(['/login']);
  }
}
