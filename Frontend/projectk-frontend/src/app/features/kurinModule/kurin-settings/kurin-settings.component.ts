import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { ButtonModule } from 'primeng/button';
import { MessageModule } from 'primeng/message';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { KurinDto } from '../common/models/kurinDto';
import { KurinService } from '../common/services/kurin-service/kurin.service';

@Component({
  selector: 'app-kurin-settings',
  imports: [
    FormsModule,
    ButtonModule,
    MessageModule,
    ToggleSwitchModule
  ],
  templateUrl: './kurin-settings.component.html',
  styleUrls: ['./kurin-settings.component.css']
})
export class KurinSettingsComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly kurinService = inject(KurinService);

  kurinKey = '';
  kurin: KurinDto | null = null;
  profileVerificationEnabled = false;
  loading = true;
  saving = false;
  errorMessage: string | null = null;

  ngOnInit(): void {
    this.kurinKey = this.route.snapshot.paramMap.get('kurinKey') ?? '';

    if (!this.kurinKey) {
      this.errorMessage = 'Не вдалося визначити курінь для налаштувань.';
      this.loading = false;
      return;
    }

    this.loadSettings();
  }

  loadSettings(): void {
    this.loading = true;
    this.errorMessage = null;

    this.kurinService.getByKey(this.kurinKey).subscribe({
      next: (kurin) => {
        this.kurin = kurin;
        this.profileVerificationEnabled = kurin.profileVerificationEnabled ?? false;
        this.loading = false;
      },
      error: (error) => {
        console.error('Error loading kurin settings:', error);
        this.errorMessage = 'Не вдалося завантажити налаштування куреня.';
        this.loading = false;
      }
    });
  }

  save(): void {
    if (!this.kurin || this.saving) return;

    const request: KurinDto = {
      ...this.kurin,
      profileVerificationEnabled: this.profileVerificationEnabled
    };

    this.saving = true;
    this.errorMessage = null;

    this.kurinService.updateKurin(request).subscribe({
      next: (updated) => {
        this.kurin = updated;
        this.profileVerificationEnabled = updated.profileVerificationEnabled ?? false;
        this.saving = false;
      },
      error: (error) => {
        console.error('Error saving kurin settings:', error);
        this.errorMessage = 'Не вдалося зберегти налаштування куреня.';
        this.saving = false;
      }
    });
  }

  cancel(): void {
    this.router.navigate(['/kurin']);
  }
}
