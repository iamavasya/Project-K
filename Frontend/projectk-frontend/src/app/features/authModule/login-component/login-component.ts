import { Component, inject } from '@angular/core';
import { InputTextModule } from 'primeng/inputtext';
import { FormsModule } from '@angular/forms';
import { FloatLabel } from 'primeng/floatlabel';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../services/authService/auth.service';
import { PermissionService } from '../services/permission.service';
import { LoginRequest } from '../models/login-request.model';
import { Router, RouterLink } from '@angular/router';
import { InputOtpModule } from 'primeng/inputotp';
import { CommonModule } from '@angular/common';
import { MessageService } from 'primeng/api';

@Component({
  selector: 'app-login-component',
  imports: [InputTextModule, FormsModule, FloatLabel, PasswordModule, ButtonModule, InputOtpModule, CommonModule, RouterLink],
  templateUrl: './login-component.html',
  styleUrl: './login-component.css'
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly permissionService = inject(PermissionService);
  private readonly router = inject(Router);
  private readonly messageService = inject(MessageService);

  email = '';
  password = '';
  showOtpInput = false;
  otpValue = '';
  loading = false;
  useRecoveryCode = false;

  get canSubmit(): boolean {
    if (this.loading) {
      return false;
    }

    return this.showOtpInput
      ? this.otpValue.trim().length > 0
      : this.email.trim().length > 0 && this.password.length > 0;
  }

  onSubmit(): void {
    if (!this.canSubmit) {
      return;
    }

    if (this.showOtpInput) {
      this.verifyOtp();
      return;
    }

    const loginRequest: LoginRequest = {
      email: this.email,
      password: this.password
    };

    this.loading = true;
    this.authService.login(loginRequest).subscribe({
      next: (response) => {
        this.loading = false;
        if (response.requiresMfa) {
          this.showOtpInput = true;
          this.otpValue = '';
          this.useRecoveryCode = false;
        } else {
          this.navigateToPanel();
        }
      },
      error: (error) => {
        this.loading = false;
        this.showError(error.error?.message || error.message || 'Не вдалося увійти.');
      }
    });
  }

  verifyOtp(): void {
    this.loading = true;
    this.authService.verifyMfaLogin(this.email, this.otpValue).subscribe({
      next: () => {
        this.loading = false;
        this.navigateToPanel();
      },
      error: (error) => {
        this.loading = false;
        this.showError(error.error?.message || error.message || 'Невірний код підтвердження.');
      }
    });
  }

  onOtpChange(value: string | number | null): void {
    this.otpValue = String(value ?? '').replace(/\D/g, '').slice(0, 6);
  }

  onRecoveryCodeChange(value: string): void {
    this.otpValue = value;
  }

  backToPassword(): void {
    this.showOtpInput = false;
    this.otpValue = '';
    this.useRecoveryCode = false;
  }

  toggleRecoveryCode(): void {
    this.useRecoveryCode = !this.useRecoveryCode;
    this.otpValue = '';
  }

  private navigateToPanel(): void {
    const state = this.authService.getAuthStateValue();
    const isAdmin = this.permissionService.isAdmin(state?.role);
    
    if (isAdmin) {
      this.router.navigate(['/panel']);
    } else if (state?.memberKey) {
      this.router.navigate(['/member', state.memberKey]);
    } else if (state?.kurinKey) {
      this.router.navigate(['/kurin']);
    }
    else {
      this.router.navigate(['/']);
    }
  }

  private showError(detail: string): void {
    this.messageService.add({
      severity: 'error',
      summary: 'Вхід',
      detail
    });
  }
}
