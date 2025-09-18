import { Component, inject } from '@angular/core';
import { InputTextModule } from 'primeng/inputtext';
import { FormsModule } from '@angular/forms';
import { FloatLabel } from 'primeng/floatlabel';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../services/auth.service';
import { LoginRequest } from '../models/login-request.model';

@Component({
  selector: 'app-login-component',
  imports: [InputTextModule, FormsModule, FloatLabel, PasswordModule, ButtonModule],
  templateUrl: './login-component.html',
  styleUrl: './login-component.scss'
})
export class LoginComponent {
  private readonly authService = inject(AuthService);

  email: string = '';
  password: string = '';
  

  onSubmit() {
    const LoginRequest: LoginRequest = {
      email: this.email,
      password: this.password
    };
    this.authService.login(LoginRequest).subscribe({
      next: (response) => {
        // TODO: Продовжити роботу з логіном і решту auth module
        alert(`Login successful: ${response}`);
      },
      error: (error) => {
        alert(`Login failed: ${error}`);
      }
    });
  }
}
