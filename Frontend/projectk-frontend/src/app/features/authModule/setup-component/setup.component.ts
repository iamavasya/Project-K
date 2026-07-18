import { Component, inject } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { FloatLabel } from 'primeng/floatlabel';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { PasswordModule } from 'primeng/password';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { AuthService, InitializeSetupRequest } from '../services/authService/auth.service';
import { authenticatedHomeRoute } from '../functions/authenticated-home-route';

@Component({
  selector: 'app-setup',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    FloatLabel,
    InputTextModule,
    MessageModule,
    PasswordModule,
    ToggleSwitchModule
  ],
  templateUrl: './setup.component.html'
})
export class SetupComponent {
  setupForm: FormGroup;
  isSubmitting = false;
  error: string | null = null;

  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);

  constructor() {
    this.setupForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      password: ['', [Validators.required, Validators.minLength(8)]],
      enforcePrivilegedMfa: [false],
      seedDemoData: [false]
    });
  }

  onSubmit(): void {
    if (this.setupForm.invalid || this.isSubmitting) {
      return;
    }

    this.isSubmitting = true;
    this.error = null;

    const request: InitializeSetupRequest = this.setupForm.getRawValue();
    this.authService.initializeSetup(request).subscribe({
      next: () => {
        const authState = this.authService.getAuthStateValue();
        this.router.navigate(authState ? authenticatedHomeRoute(authState) : ['/']);
      },
      error: (err) => {
        let errorMessage = 'Не вдалося ініціалізувати систему. Спробуйте ще раз.';
        if (err.error && typeof err.error === 'object' && err.error.message) {
          errorMessage = err.error.message;
        } else if (err.error && typeof err.error === 'string') {
          errorMessage = err.error;
        } else if (err.message) {
          errorMessage = err.message;
        }

        this.error = errorMessage;
        this.isSubmitting = false;
      }
    });
  }
}
