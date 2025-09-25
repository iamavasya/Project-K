import { Component, inject } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-logout-component',
  imports: [ButtonModule],
  templateUrl: './logout-component.html',
  styleUrl: './logout-component.css'
})
export class LogoutComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  logout() {
    this.authService.logout().subscribe({
      next: (message) => {
        alert(message);
        this.router.navigate(['/login']);
      },
      error: (error) => {
        console.log(`Logout failed: ${error}`);
      }
    });
  }
}
