import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { MemberLookupDto } from '../../models/requests/member/memberLookupDto';
import { MemberWarningDto } from '../../models/memberWarningDto';
import { MemberWarningLevel } from '../../models/enums/member-warning-level.enum';
import { ProfileVerificationBadgeComponent } from '../profile-verification-badge/profile-verification-badge';
import { parseUtcDateTime } from '../../../../../shared/functions/utcDateTime.function';

@Component({
  selector: 'app-mini-member-card',
  imports: [CommonModule, ButtonModule, TagModule, TooltipModule, ProfileVerificationBadgeComponent],
  templateUrl: './mini-member-card.html',
  styleUrl: './mini-member-card.css'
})
export class MiniMemberCardComponent {
  @Input({ required: true }) member!: MemberLookupDto;
  @Output() navigate = new EventEmitter<MemberLookupDto>();

  readonly warningLevels = [
    MemberWarningLevel.Level1,
    MemberWarningLevel.Level2,
    MemberWarningLevel.Level3
  ];

  onNavigate(): void {
    this.navigate.emit(this.member);
  }

  get hasActiveWarnings(): boolean {
    return this.getActiveWarnings().length > 0;
  }

  get warningTooltip(): string {
    const activeWarning = this.getActiveWarnings()
      .sort((left, right) => this.getWarningLevelWeight(right.level) - this.getWarningLevelWeight(left.level))[0];

    if (!activeWarning) {
      return '';
    }

    const levelLabel = this.getWarningLevelLabel(activeWarning.level);
    const daysLeft = this.getWarningDaysLeft(activeWarning.expiresAtUtc);
    return `Поточна пересторога: ${levelLabel}. Залишилось днів: ${daysLeft}.`;
  }

  getWarningDotActive(level: MemberWarningLevel): boolean {
    const activeLevel = this.getActiveWarningLevel();
    if (!activeLevel) {
      return false;
    }

    return this.getWarningLevelWeight(level) <= this.getWarningLevelWeight(activeLevel);
  }


  private getWarningLevelWeight(level: MemberWarningLevel): number {
    switch (level) {
      case MemberWarningLevel.Level3: return 3;
      case MemberWarningLevel.Level2: return 2;
      case MemberWarningLevel.Level1: return 1;
      default: return 0;
    }
  }

  private getActiveWarnings(): MemberWarningDto[] {
    const warnings = this.member?.warnings ?? [];
    const now = Date.now();
    return warnings.filter(warning =>
      !warning.revokedAtUtc && this.parseUtcDate(warning.expiresAtUtc) > now
    );
  }

  private getActiveWarningLevel(): MemberWarningLevel | null {
    const activeWarnings = this.getActiveWarnings();
    if (!activeWarnings.length) {
      return null;
    }

    return activeWarnings
      .map(warning => warning.level)
      .sort((left, right) => this.getWarningLevelWeight(right) - this.getWarningLevelWeight(left))[0];
  }

  private parseUtcDate(value: string | null | undefined): number {
    if (!value) {
      return 0;
    }

    return parseUtcDateTime(value)?.getTime() ?? 0;
  }

  private getWarningLevelLabel(level: MemberWarningLevel): string {
    switch (level) {
      case MemberWarningLevel.Level1:
        return 'Перша';
      case MemberWarningLevel.Level2:
        return 'Друга';
      case MemberWarningLevel.Level3:
        return 'Третя';
      default:
        return 'Пересторога';
    }
  }

  private getWarningDaysLeft(expiresAtUtc: string): number {
    const expiresAt = this.parseUtcDate(expiresAtUtc);
    const diffMs = expiresAt - Date.now();
    return Math.max(0, Math.ceil(diffMs / 86400000));
  }
}
