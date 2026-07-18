import { Component, inject, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../../../environments/environment';
import { ToggleSwitchModule, ToggleSwitchChangeEvent } from 'primeng/toggleswitch';
import { ButtonModule } from 'primeng/button';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';

const ENFORCE_PRIVILEGED_MFA_KEY = 'Security__EnforcePrivilegedMFA';

@Component({
  selector: 'app-system-settings',
  standalone: true,
  imports: [ToggleSwitchModule, ButtonModule, FormsModule],
  templateUrl: './system-settings.component.html',
  styleUrl: './system-settings.component.css'
})
export class SystemSettingsComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messageService = inject(MessageService);
  private readonly router = inject(Router);
  private readonly apiUrl = environment.apiUrl;

  enforcePrivilegedMfa = false;
  loading = false;

  ngOnInit() {
    this.loadSettings();
  }

  back() {
    this.router.navigate(['/panel']);
  }

  loadSettings() {
    this.loading = true;
    this.http.get<Record<string, string>>(`${this.apiUrl}/settings`, { withCredentials: true }).subscribe({
      next: (settings) => {
        this.enforcePrivilegedMfa = settings?.[ENFORCE_PRIVILEGED_MFA_KEY] === 'true';
        this.loading = false;
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Помилка', detail: 'Не вдалося завантажити налаштування' });
        this.loading = false;
      }
    });
  }

  onMfaToggle(event: ToggleSwitchChangeEvent) {
    this.loading = true;
    const value = event.checked ? 'true' : 'false';
    this.http.put(`${this.apiUrl}/settings/${ENFORCE_PRIVILEGED_MFA_KEY}`, { value }, { withCredentials: true }).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Збережено', detail: 'Налаштування оновлено' });
        this.loading = false;
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Помилка', detail: 'Не вдалося оновити налаштування' });
        this.enforcePrivilegedMfa = !event.checked;
        this.loading = false;
      }
    });
  }
}
