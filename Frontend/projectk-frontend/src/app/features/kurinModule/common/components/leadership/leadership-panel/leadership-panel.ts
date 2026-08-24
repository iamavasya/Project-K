import { DatePipe } from '@angular/common';
import { Component, Input, OnChanges, inject, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ButtonModule } from '@openng/optimus-ui/button';
import { IconFieldModule } from '@openng/optimus-ui/iconfield';
import { InputIconModule } from '@openng/optimus-ui/inputicon';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { TableModule } from '@openng/optimus-ui/table';
import { TagModule } from '@openng/optimus-ui/tag';
import { ToggleSwitchModule } from '@openng/optimus-ui/toggleswitch';
import { TooltipModule } from '@openng/optimus-ui/tooltip';
import { PermissionService } from '../../../../../authModule/services/permission.service';
import { LeadershipDto, LeadershipHistoryDto } from '../../../models/requests/leadership/leadershipDto';
import { LeadershipRole } from '../../../models/enums/leadership-role.enum';
import { ROLE_DISPLAY_NAMES } from '../../../models/roleDisplayName';
import { LeadershipService } from '../../../services/leadership-service/leadership-service';
import { MemberLookupDto } from '../../../models/requests/member/memberLookupDto';
import { compareLeadershipHistoriesByDefault } from '../../../functions/leadershipRoleOrder.function';
import { EmptyStateComponent } from '../../../../../../shared/empty-state/empty-state';

@Component({
  selector: 'app-leadership-panel',
  imports: [
    FormsModule,
    ButtonModule,
    IconFieldModule,
    InputIconModule,
    InputTextModule,
    TableModule,
    TagModule,
    ToggleSwitchModule,
    TooltipModule,
    EmptyStateComponent,
    DatePipe
],
  templateUrl: './leadership-panel.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './leadership-panel.css'
})
export class LeadershipPanelComponent implements OnChanges {
  readonly archiveScrollHeight = '27.5rem';

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
      .sort(compareLeadershipHistoriesByDefault);
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
}
