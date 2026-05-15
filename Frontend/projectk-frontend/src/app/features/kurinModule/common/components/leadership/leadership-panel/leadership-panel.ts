import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { InputTextModule } from 'primeng/inputtext';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { TooltipModule } from 'primeng/tooltip';
import { PermissionService } from '../../../../../authModule/services/permission.service';
import { LeadershipDto, LeadershipHistoryDto } from '../../../models/requests/leadership/leadershipDto';
import { LeadershipRole } from '../../../models/enums/leadership-role.enum';
import { ROLE_DISPLAY_NAMES } from '../../../models/roleDisplayName';
import { LeadershipService } from '../../../services/leadership-service/leadership-service';
import { MemberLookupDto } from '../../../models/requests/member/memberLookupDto';

@Component({
  selector: 'app-leadership-panel',
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    TableModule,
    TagModule,
    ToggleSwitchModule,
    TooltipModule
  ],
  templateUrl: './leadership-panel.html',
  styleUrl: './leadership-panel.css'
})
export class LeadershipPanelComponent implements OnChanges {
  @Input() leadershipType: 'kurin' | 'group' = 'group';
  @Input() typeKey = '';

  private readonly leadershipService = inject(LeadershipService);
  private readonly permissionService = inject(PermissionService);
  private readonly router = inject(Router);

  leadership: LeadershipDto | null = null;
  histories: LeadershipHistoryDto[] = [];
  showArchived = false;
  searchTerm = '';
  isLoading = false;

  ngOnChanges(): void {
    if (this.typeKey) {
      this.loadLeadership();
    }
  }

  get canSetupLeadership(): boolean {
    return this.permissionService.canSetupLeadership();
  }

  get title(): string {
    return this.leadershipType === 'kurin' ? 'Провід куреня' : 'Провід гуртка';
  }

  get visibleHistories(): LeadershipHistoryDto[] {
    const search = this.searchTerm.trim().toLowerCase();
    return this.histories
      .filter(history => this.showArchived || !history.endDate)
      .filter(history => {
        if (!search) {
          return true;
        }

        const name = this.getMemberName(history.member).toLowerCase();
        const role = this.getRoleDisplayName(history.role).toLowerCase();
        return name.includes(search) || role.includes(search);
      })
      .sort((a, b) => {
        if (!a.endDate && b.endDate) return -1;
        if (a.endDate && !b.endDate) return 1;
        return this.getDateTime(b.startDate) - this.getDateTime(a.startDate);
      });
  }

  loadLeadership(): void {
    this.isLoading = true;
    this.leadershipService.getLeadershipByTypeAndKey(this.leadershipType, this.typeKey).subscribe({
      next: (leadership) => {
        this.leadership = leadership;
        this.histories = leadership?.leadershipHistories ?? [];
        this.isLoading = false;
      },
      error: () => {
        this.leadership = null;
        this.histories = [];
        this.isLoading = false;
      }
    });
  }

  onSettingsSelect(): void {
    if (this.leadership) {
      this.router.navigate(['/leadership', this.leadership.leadershipKey, this.leadershipType, this.typeKey]);
      return;
    }

    this.router.navigate(['/leadership/create', this.leadershipType, this.typeKey]);
  }

  onMemberSelect(member: MemberLookupDto): void {
    if (member?.memberKey) {
      this.router.navigate(['/member', member.memberKey]);
    }
  }

  getMemberName(member: MemberLookupDto): string {
    return `${member.lastName} ${member.firstName}${member.middleName ? ` ${member.middleName}` : ''}`;
  }

  getRoleDisplayName(role: string): string {
    return ROLE_DISPLAY_NAMES[role as LeadershipRole] || role;
  }

  getRoleSeverity(history: LeadershipHistoryDto): 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' | undefined | null {
    if (history.endDate) {
      return 'secondary';
    }

    switch (history.role as LeadershipRole) {
      case LeadershipRole.Kurinnuy:
      case LeadershipRole.Hurtkoviy:
        return 'danger';
      case LeadershipRole.Suddya:
        return 'warn';
      case LeadershipRole.Skarbnyk:
        return 'success';
      default:
        return 'info';
    }
  }

  private getDateTime(value?: string | null): number {
    return value ? new Date(value).getTime() : 0;
  }
}
