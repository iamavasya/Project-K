import { Component, inject, Input, OnInit } from '@angular/core';
import { MemberService } from '../../services/member-service/member.service';
import { TableModule } from 'primeng/table';
import { InputIconModule } from 'primeng/inputicon';
import { IconFieldModule } from 'primeng/iconfield';
import { InputTextModule } from 'primeng/inputtext';
import { Router } from '@angular/router';
import { LeadershipService } from '../../services/leadership-service/leadership-service';
import { LeadershipDto, LeadershipHistoryDto } from '../../models/requests/leadership/leadershipDto';
import { MemberLookupDto } from '../../models/requests/member/memberLookupDto';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { DatePipe, CommonModule } from '@angular/common';
import { LeadershipRole } from '../../models/enums/leadership-role.enum';
import { ROLE_DISPLAY_NAMES } from '../../models/roleDisplayName';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { FormsModule } from '@angular/forms';
import { MiniMemberCardComponent } from '../mini-member-card/mini-member-card';
import { UpcomingBirthdaysTileComponent } from '../upcoming-birthdays-tile/upcoming-birthdays-tile';
import { buildUpcomingBirthdays } from '../../functions/upcomingBirthdays.function';

import { AuthService } from '../../../../authModule/services/authService/auth.service';

@Component({
  selector: 'app-member-list',
  imports: [
    CommonModule,
    TableModule,
    InputIconModule,
    IconFieldModule,
    InputTextModule,
    ButtonModule,
    TagModule,
    TooltipModule,
    ToggleSwitchModule,
    FormsModule,
    MiniMemberCardComponent,
    UpcomingBirthdaysTileComponent
],
  templateUrl: './member-list.html',
  styleUrl: './member-list.css',
  providers: [DatePipe]
})
export class MemberList implements OnInit {
  @Input() type: 'kurin' | 'group' | 'leadership' = 'group';
  @Input() leadershipType: 'kurin' | 'group' | 'kv' = 'group';
  @Input() typeKey = '';

  private readonly groupCardViewStorageKeyPrefix = 'member-list:group-card-view';
  private readonly upcomingBirthdaysWindowDays = 30;

  private readonly memberService = inject(MemberService);
  private readonly leadershipService = inject(LeadershipService);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);

  get canSetupLeadership(): boolean {
    const role = this.authService.getAuthStateValue()?.role?.trim().toLowerCase();
    return role !== 'user';
  }

  membersLookup: MemberLookupDto[] = [];
  
  leadership: LeadershipDto | null = null;
  leadershipHistories: LeadershipHistoryDto[] = [];
  allHistories: LeadershipHistoryDto[] = [];

  showArchived = false;
  showGroupCardView = false;
  hasUpcomingBirthdays = false;
  memberSearchQuery = '';

  selectedMember: MemberLookupDto | null = null;

  ngOnInit(): void {
    if (!this.type || !this.typeKey) return;

    switch (this.type) {
      case 'kurin':
        this.loadMembers();
        break;
      case 'group':
        this.restoreGroupCardViewState();
        this.loadMembers();
        break;
      case 'leadership':
        this.loadLeadership();
        break;
    }
  }

  private loadMembers(): void {
    const request$ = this.type === 'kurin' 
      ? this.memberService.getAll(undefined, this.typeKey)
      : this.memberService.getAll(this.typeKey);

    request$.subscribe({
      next: (members) => {
        this.membersLookup = members.map(m => ({
          memberKey: m.memberKey,
          firstName: m.firstName,
          lastName: m.lastName,
          middleName: m.middleName,
          profilePhotoUrl: m.profilePhotoUrl,
          latestPlastLevel: m.latestPlastLevel ?? null,
          latestPlastLevelDisplay: m.latestPlastLevelDisplay ?? null,
          phoneNumber: m.phoneNumber,
          dateOfBirth: m.dateOfBirth
        }));
        this.hasUpcomingBirthdays = buildUpcomingBirthdays(this.membersLookup, this.upcomingBirthdaysWindowDays).length > 0;
      }
    });
  }

  get upcomingBirthdaysDaysAhead(): number {
    return this.upcomingBirthdaysWindowDays;
  }

  get filteredMembersLookup(): MemberLookupDto[] {
    const query = this.memberSearchQuery.trim().toLowerCase();
    if (!query) {
      return this.membersLookup;
    }

    return this.membersLookup.filter(member => {
      const fullName = `${member.lastName} ${member.firstName} ${member.middleName ?? ''}`.toLowerCase();
      const latestPlastLevel = (member.latestPlastLevelDisplay ?? member.latestPlastLevel ?? '').toLowerCase();
      const phoneNumber = (member.phoneNumber ?? '').toLowerCase();

      return fullName.includes(query)
        || latestPlastLevel.includes(query)
        || phoneNumber.includes(query);
    });
  }

  onGroupCardViewToggleChange(): void {
    if (this.type !== 'group') {
      return;
    }

    this.persistGroupCardViewState();
  }

  private getGroupCardViewStorageKey(): string {
    return `${this.groupCardViewStorageKeyPrefix}:${this.typeKey}`;
  }

  private restoreGroupCardViewState(): void {
    if (typeof window === 'undefined') {
      return;
    }

    try {
      const savedValue = window.sessionStorage.getItem(this.getGroupCardViewStorageKey());
      if (savedValue === null) {
        return;
      }

      this.showGroupCardView = savedValue === 'true';
    } catch {
      // Ignore storage access errors (private mode / blocked storage).
    }
  }

  private persistGroupCardViewState(): void {
    if (typeof window === 'undefined') {
      return;
    }

    try {
      window.sessionStorage.setItem(this.getGroupCardViewStorageKey(), String(this.showGroupCardView));
    } catch {
      // Ignore storage access errors (private mode / blocked storage).
    }
  }

  private loadLeadership(): void {
    this.leadershipService.getLeadershipByTypeAndKey(this.leadershipType, this.typeKey).subscribe({
      next: (leadership) => {
        this.leadership = leadership;
        this.allHistories = leadership.leadershipHistories;
        this.refreshList();
      }
    });
  }

  refreshList(): void {
    let filtered = [...this.allHistories];
    if (!this.showArchived) {
      filtered = filtered.filter(h => !h.endDate);
    }
    filtered.sort((a, b) => {
      if (!a.endDate && b.endDate) return -1;
      if (a.endDate && !b.endDate) return 1;
      return new Date(b.startDate).getTime() - new Date(a.startDate).getTime();
    });
    this.leadershipHistories = filtered.map(h => ({
      ...h,
      roleNameUA: this.getRoleDisplayName(h.role)
    }));
  }

  onMemberSelect(member: MemberLookupDto): void {
    if (member) {
      this.router.navigate(['/member', member.memberKey]);
    }
  }

  onLeadershipSettingsSelect(): void {
    if (this.leadership) {
      this.router.navigate(['/leadership', this.leadership.leadershipKey, this.leadershipType, this.typeKey]);
    } else if (this.type && this.typeKey) {
      this.router.navigate(['/leadership/create', this.leadershipType, this.typeKey]);
    }
  }

  getRoleDisplayName(role: string): string {
    return ROLE_DISPLAY_NAMES[role as LeadershipRole] || role;
  }

  getRoleSeverity(history: LeadershipHistoryDto): 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' | undefined | null {
    if (history.endDate) {
        return 'secondary'; 
    }

    const role = history.role as LeadershipRole;
    switch (role) {
        case LeadershipRole.Kurinnuy:
        case LeadershipRole.Hurtkoviy:
        case LeadershipRole.Zvyazkovyi:
            return 'danger';
        case LeadershipRole.Suddya:
            return 'warn';
        default:
            return 'info';
    }
  }
}