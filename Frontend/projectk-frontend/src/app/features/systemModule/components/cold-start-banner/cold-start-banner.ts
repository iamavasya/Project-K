import { Component, inject } from '@angular/core';
import { HealthBannerService } from '../../services/health-banner.service';

@Component({
  selector: 'app-cold-start-banner',
  templateUrl: './cold-start-banner.html',
  styleUrl: './cold-start-banner.css'
})
export class ColdStartBannerComponent {
  private readonly healthBannerService = inject(HealthBannerService);

  protected readonly isVisible = this.healthBannerService.isBannerVisible;
}
