import { Component, Input } from '@angular/core';
import { TooltipModule } from 'primeng/tooltip';
import { MemberProfileVerificationStatus } from '../../models/enums/member-profile-verification-status.enum';

@Component({
  selector: 'app-profile-verification-badge',
  imports: [TooltipModule],
  templateUrl: './profile-verification-badge.html',
  styleUrl: './profile-verification-badge.css'
})
export class ProfileVerificationBadgeComponent {
  @Input() status: MemberProfileVerificationStatus | string | null | undefined;
  @Input() enabled = true;

  get isVisible(): boolean {
    return this.enabled && (this.isCurrent || this.isStale);
  }

  get isCurrent(): boolean {
    return this.status === MemberProfileVerificationStatus.VerifiedCurrent;
  }

  get isStale(): boolean {
    return this.status === MemberProfileVerificationStatus.VerifiedStale;
  }

  get tooltip(): string | undefined {
    if (this.isCurrent) {
      return 'Дані верифіковано';
    }

    if (this.isStale) {
      return 'Дані змінено після верифікації';
    }

    return undefined;
  }
}
