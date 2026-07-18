import { Component, inject, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../../environments/environment';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { FormsModule } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-system-settings',
  standalone: true,
  imports: [CommonModule, ToggleSwitchModule, FormsModule],
  templateUrl: './system-settings.component.html'
})
export class SystemSettingsComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly messageService = inject(MessageService);
  private readonly apiUrl = environment.apiUrl;

  enforcePrivilegedMfa = false;
  loading = false;

  ngOnInit() {
    this.loadSettings();
  }

  loadSettings() {
    this.loading = true;
    this.http.get<any>(`${this.apiUrl}/settings`, { withCredentials: true }).subscribe({
      next: (res) => {
        const settings = res.data || {};
        this.enforcePrivilegedMfa = settings['Security__EnforcePrivilegedMFA'] === 'true';
        this.loading = false;
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to load settings' });
        this.loading = false;
      }
    });
  }

  onMfaToggle(event: any) {
    this.loading = true;
    const value = event.checked ? 'true' : 'false';
    this.http.put<any>(`${this.apiUrl}/settings/Security__EnforcePrivilegedMFA`, { value }, { withCredentials: true }).subscribe({
      next: () => {
        this.messageService.add({ severity: 'success', summary: 'Success', detail: 'Setting updated successfully' });
        this.loading = false;
      },
      error: () => {
        this.messageService.add({ severity: 'error', summary: 'Error', detail: 'Failed to update setting' });
        // Revert change
        this.enforcePrivilegedMfa = !event.checked;
        this.loading = false;
      }
    });
  }
}
