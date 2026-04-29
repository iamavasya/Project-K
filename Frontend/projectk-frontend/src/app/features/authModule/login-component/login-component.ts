import { Component, inject } from '@angular/core';
import { InputTextModule } from 'primeng/inputtext';
import { FormsModule } from '@angular/forms';
import { FloatLabel } from 'primeng/floatlabel';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../services/authService/auth.service';
import { LoginRequest } from '../models/login-request.model';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login-component',
  imports: [InputTextModule, FormsModule, FloatLabel, PasswordModule, ButtonModule],
  templateUrl: './login-component.html',
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  email = '';
  password = '';

  onSubmit() {
    const LoginRequest: LoginRequest = {
      email: this.email,
      password: this.password
    };
    this.authService.login(LoginRequest).subscribe({
      next: (response) => {
        const state = this.authService.getAuthStateValue();
        const role = state?.role?.trim().toLowerCase();
        
        if (role === 'admin') {
          this.router.navigate(['/panel']);
        } else if (state?.memberKey) {
          this.router.navigate(['/member', state.memberKey]);
        } else if (state?.kurinKey) {
          this.router.navigate(['/kurin']);
        }
        else {
          this.router.navigate(['/']);
        }
      },
      error: (error) => {
        alert(`Login failed: ${error}`);
      }
    });
  }
}
