import { CommonModule } from '@angular/common';
import { Component, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DividerModule } from 'primeng/divider';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { TagModule } from 'primeng/tag';
import { MfaSetupDialogComponent } from '../components/mfa-setup-dialog/mfa-setup-dialog.component';
import { AccountSettings, AccountSettingsService } from '../services/account-settings.service';
import { AuthService } from '../services/authService/auth.service';
import { PermissionService } from '../services/permission.service';

@Component({
  selector: 'app-account-settings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    InputTextModule,
    PasswordModule,
    ButtonModule,
    DividerModule,
    TagModule,
    MfaSetupDialogComponent
  ],
  templateUrl: './account-settings.component.html',
  styleUrl: './account-settings.component.css'
})
export class AccountSettingsComponent implements OnInit {
  private readonly accountService = inject(AccountSettingsService);
  private readonly authService = inject(AuthService);
  private readonly messageService = inject(MessageService);
  private readonly permissionService = inject(PermissionService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  @ViewChild('mfaSetupDialog') mfaSetupDialog?: MfaSetupDialogComponent;

  settings: AccountSettings | null = null;
  loading = false;
  savingProfile = false;
  changingPassword = false;
  resettingMfa = false;
  confirmingEmail = false;
  pendingEmail: string | null = null;

  profileForm = {
    email: '',
    phoneNumber: '',
    currentPassword: ''
  };

  passwordForm = {
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  };

  mfaResetPassword = '';
  recoveryCodesPassword = '';
  recoveryCodes: string[] = [];
  rotatingRecoveryCodes = false;

  get canSaveProfile(): boolean {
    return this.profileForm.email.trim().length > 0
      && (!this.isEmailChanged || this.profileForm.currentPassword.length > 0)
      && !this.savingProfile
      && !this.confirmingEmail;
  }

  get canChangePassword(): boolean {
    return this.passwordForm.currentPassword.length > 0
      && this.passwordForm.newPassword.length >= 8
      && this.passwordForm.newPassword === this.passwordForm.confirmPassword
      && !this.changingPassword;
  }

  get isPrivileged(): boolean {
    return this.permissionService.isAdmin(this.settings?.role) || this.permissionService.isManager(this.settings?.role);
  }

  get isEmailChanged(): boolean {
    return !!this.settings
      && this.profileForm.email.trim().toLowerCase() !== this.settings.email.toLowerCase();
  }

  get canResetMfa(): boolean {
    return !!this.settings?.twoFactorEnabled
      && this.isPrivileged
      && this.mfaResetPassword.length > 0
      && !this.resettingMfa;
  }

  get canDisableMfa(): boolean {
    return !!this.settings?.twoFactorEnabled
      && !this.isPrivileged
      && this.mfaResetPassword.length > 0
      && !this.resettingMfa;
  }

  get canRotateRecoveryCodes(): boolean {
    return !!this.settings?.twoFactorEnabled
      && this.recoveryCodesPassword.length > 0
      && !this.rotatingRecoveryCodes;
  }

  ngOnInit(): void {
    if (!this.confirmEmailFromRoute()) {
      this.load();
    }
  }

  load(): void {
    this.loading = true;
    this.accountService.getSettings().subscribe({
      next: settings => {
        this.applySettings(settings);
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.showError('Не вдалося завантажити налаштування акаунта.');
      }
    });
  }

  saveProfile(): void {
    if (!this.canSaveProfile) {
      return;
    }

    this.savingProfile = true;
    this.accountService.updateProfile({
      email: this.profileForm.email.trim(),
      phoneNumber: this.profileForm.phoneNumber.trim() || null,
      currentPassword: this.isEmailChanged ? this.profileForm.currentPassword : null
    }).subscribe({
      next: settings => {
        this.applySettings(settings);
        this.profileForm.currentPassword = '';
        this.savingProfile = false;

        if (settings.pendingEmail) {
          this.messageService.add({
            severity: 'info',
            summary: 'Акаунт',
            detail: `Ми надіслали лист підтвердження на ${settings.pendingEmail}. Поточний email зміниться після підтвердження.`
          });
          return;
        }

        this.authService.updateEmail(settings.email);
        this.messageService.add({ severity: 'success', summary: 'Акаунт', detail: 'Контактні дані оновлено.' });
      },
      error: () => {
        this.savingProfile = false;
        this.showError('Не вдалося оновити контактні дані. Перевірте email.');
      }
    });
  }

  changePassword(): void {
    if (!this.canChangePassword) {
      return;
    }

    this.changingPassword = true;
    this.accountService.changePassword({
      currentPassword: this.passwordForm.currentPassword,
      newPassword: this.passwordForm.newPassword
    }).subscribe({
      next: () => {
        this.passwordForm = { currentPassword: '', newPassword: '', confirmPassword: '' };
        this.changingPassword = false;
        this.messageService.add({ severity: 'success', summary: 'Акаунт', detail: 'Пароль змінено.' });
      },
      error: () => {
        this.changingPassword = false;
        this.showError('Не вдалося змінити пароль. Перевірте поточний пароль і вимоги до нового.');
      }
    });
  }

  resetMfa(): void {
    if (!this.canResetMfa) {
      return;
    }

    this.resettingMfa = true;
    this.accountService.resetMfa({ currentPassword: this.mfaResetPassword }).subscribe({
      next: () => {
        if (this.settings) {
          this.settings = { ...this.settings, twoFactorEnabled: false };
        }

        this.mfaResetPassword = '';
        this.resettingMfa = false;
        this.messageService.add({
          severity: 'success',
          summary: 'MFA',
          detail: 'MFA скинуто. Налаштуйте authenticator заново.'
        });

        if (this.isPrivileged) {
          this.setupMfa(true);
        }
      },
      error: () => {
        this.resettingMfa = false;
        this.showError('Не вдалося скинути MFA.');
      }
    });
  }

  disableMfa(): void {
    if (!this.canDisableMfa) {
      return;
    }

    this.resettingMfa = true;
    this.accountService.disableMfa({ currentPassword: this.mfaResetPassword }).subscribe({
      next: () => {
        if (this.settings) {
          this.settings = { ...this.settings, twoFactorEnabled: false };
        }

        this.mfaResetPassword = '';
        this.resettingMfa = false;
        this.messageService.add({
          severity: 'success',
          summary: 'MFA',
          detail: 'MFA disabled.'
        });
      },
      error: () => {
        this.resettingMfa = false;
        this.showError('Unable to disable MFA. Check your current password.');
      }
    });
  }

  setupMfa(mandatory = false): void {
    this.mfaSetupDialog?.show(mandatory);
  }

  onMfaEnabled(): void {
    if (this.settings) {
      this.settings = { ...this.settings, twoFactorEnabled: true };
    }
  }

  rotateRecoveryCodes(): void {
    if (!this.canRotateRecoveryCodes) {
      return;
    }

    this.rotatingRecoveryCodes = true;
    this.authService.rotateMfaRecoveryCodes(this.recoveryCodesPassword).subscribe({
      next: response => {
        this.recoveryCodes = response.recoveryCodes ?? [];
        this.recoveryCodesPassword = '';
        this.rotatingRecoveryCodes = false;
        this.messageService.add({ severity: 'success', summary: 'MFA', detail: 'Recovery codes rotated.' });
      },
      error: () => {
        this.rotatingRecoveryCodes = false;
        this.showError('Не вдалося оновити recovery codes. Перевірте поточний пароль.');
      }
    });
  }

  private confirmEmailFromRoute(): boolean {
    const query = this.route.snapshot.queryParamMap;
    if (query.get('confirmEmail') !== 'true') {
      return false;
    }

    const email = query.get('email');
    const token = query.get('token');
    if (!email || !token) {
      this.showError('Посилання підтвердження email неповне.');
      this.clearEmailConfirmationQuery();
      return false;
    }

    this.confirmingEmail = true;
    this.accountService.confirmEmailChange({ email, token }).subscribe({
      next: settings => {
        this.applySettings(settings);
        this.authService.updateEmail(settings.email);
        this.confirmingEmail = false;
        this.clearEmailConfirmationQuery();
        this.messageService.add({ severity: 'success', summary: 'Акаунт', detail: 'Email підтверджено та оновлено.' });
      },
      error: () => {
        this.confirmingEmail = false;
        this.clearEmailConfirmationQuery();
        this.showError('Не вдалося підтвердити email. Посилання могло застаріти або вже бути використаним.');
      }
    });

    return true;
  }

  private clearEmailConfirmationQuery(): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: {},
      replaceUrl: true
    });
  }

  private applySettings(settings: AccountSettings): void {
    this.settings = settings;
    this.pendingEmail = settings.pendingEmail;
    this.profileForm = {
      email: settings.email,
      phoneNumber: settings.phoneNumber ?? '',
      currentPassword: this.profileForm.currentPassword
    };
  }

  private showError(detail: string): void {
    this.messageService.add({
      severity: 'error',
      summary: 'Акаунт',
      detail
    });
  }
}
