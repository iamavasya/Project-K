import { Component, inject, OnInit, ChangeDetectionStrategy, input } from '@angular/core';
import { MemberService } from '../../services/member-service/member.service';
import { TableModule } from '@openng/optimus-ui/table';
import { InputIconModule } from '@openng/optimus-ui/inputicon';
import { IconFieldModule } from '@openng/optimus-ui/iconfield';
import { InputTextModule } from '@openng/optimus-ui/inputtext';
import { Router } from '@angular/router';
import { LeadershipService } from '../../services/leadership-service/leadership-service';
import { LeadershipDto, LeadershipHistoryDto } from '../../models/requests/leadership/leadershipDto';
import { MemberLookupDto } from '../../models/requests/member/memberLookupDto';
import { ButtonModule } from '@openng/optimus-ui/button';
import { TagModule } from '@openng/optimus-ui/tag';
import { TooltipModule } from '@openng/optimus-ui/tooltip';
import { DatePipe } from '@angular/common';
import { LeadershipRole } from '../../models/enums/leadership-role.enum';
import { ROLE_DISPLAY_NAMES } from '../../models/roleDisplayName';
import { ToggleSwitchModule } from '@openng/optimus-ui/toggleswitch';
import { FormsModule } from '@angular/forms';
import { MiniMemberCardComponent } from '../mini-member-card/mini-member-card';
import { UpcomingBirthdaysTileComponent } from '../upcoming-birthdays-tile/upcoming-birthdays-tile';
import { buildUpcomingBirthdays } from '../../functions/upcomingBirthdays.function';
import { compareLeadershipHistoriesByDefault, getLeadershipRoleSortWeight } from '../../functions/leadershipRoleOrder.function';
import { ProfileVerificationBadgeComponent } from '../profile-verification-badge/profile-verification-badge';
import { EmptyStateComponent } from '../../../../../shared/empty-state/empty-state';

import { AuthService } from '../../../../authModule/services/authService/auth.service';
import { PermissionService } from '../../../../authModule/services/permission.service';

@Component({
  selector: 'app-member-list',
  imports: [
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
    UpcomingBirthdaysTileComponent,
    ProfileVerificationBadgeComponent,
    EmptyStateComponent,
    DatePipe
],
  templateUrl: './member-list.html',
  styleUrl: './member-list.css',
  changeDetection: ChangeDetectionStrategy.Eager,
  providers: [DatePipe]
})
export class MemberList implements OnInit {
  readonly type = input<'kurin' | 'group' | 'leadership'>('group');
  readonly leadershipType = input<'kurin' | 'group' | 'kv'>('group');
  readonly typeKey = input('');

  private readonly groupCardViewStorageKeyPrefix = 'member-list:group-card-view';
  private readonly upcomingBirthdaysWindowDays = 30;

  private readonly memberService = inject(MemberService);
  private readonly leadershipService = inject(LeadershipService);
  private readonly router = inject(Router);
  private readonly authService = inject(AuthService);
  private readonly permissionService = inject(PermissionService);

  get canSetupLeadership(): boolean {
    return this.permissionService.canSetupLeadership();
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
    const type = this.type();
    if (!type || !this.typeKey()) return;

    switch (type) {
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
    const request$ = this.type() === 'kurin' 
      ? this.memberService.getAll(undefined, this.typeKey())
      : this.memberService.getAll(this.typeKey());

    request$.subscribe({
      next: (members) => {
        this.membersLookup = members.map(m => ({
          memberKey: m.memberKey,
          userKey: m.userKey,
          userRole: m.userRole,
          firstName: m.firstName,
          lastName: m.lastName,
          middleName: m.middleName,
          fullNameSort: this.getFullNameSortValue(m),
          roleSortWeight: this.getMemberRoleSortWeight(m),
          leadershipHistories: m.leadershipHistories ?? [],
          profilePhotoUrl: m.profilePhotoUrl,
          latestPlastLevel: m.latestPlastLevel ?? null,
          latestPlastLevelDisplay: m.latestPlastLevelDisplay ?? null,
          phoneNumber: m.phoneNumber,
          dateOfBirth: m.dateOfBirth,
          profileVerificationStatus: m.profileVerificationStatus,
          profileVerifiedAtUtc: m.profileVerifiedAtUtc,
          profileVerifiedByUserKey: m.profileVerifiedByUserKey,
          profileVerificationNote: m.profileVerificationNote,
          warnings: m.warnings ?? []
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
    if (this.type() !== 'group') {
      return;
    }

    this.persistGroupCardViewState();
  }

  private getGroupCardViewStorageKey(): string {
    return `${this.groupCardViewStorageKeyPrefix}:${this.typeKey()}`;
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
    this.leadershipService.getLeadershipByTypeAndKey(this.leadershipType(), this.typeKey()).subscribe({
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
    filtered.sort(compareLeadershipHistoriesByDefault);
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
    const typeKey = this.typeKey();
    if (this.leadership) {
      this.router.navigate(['/leadership', this.leadership.leadershipKey, this.leadershipType(), this.typeKey()]);
    } else if (this.type() && typeKey) {
      this.router.navigate(['/leadership/create', this.leadershipType(), typeKey]);
    }
  }

  getRoleDisplayName(role: string): string {
    return ROLE_DISPLAY_NAMES[role as LeadershipRole] || role;
  }

  getMemberRoleTags(member: MemberLookupDto): { label: string; severity: 'success' | 'secondary' | 'info' | 'warn' | 'danger' | 'contrast' | undefined | null }[] {
    return [
      ...this.getKvRoleTags(member),
      ...(member.leadershipHistories ?? [])
      .filter(history => !history.endDate)
      .sort(compareLeadershipHistoriesByDefault)
      .map(history => ({
        label: this.getMemberRoleLabel(history),
        severity: this.getRoleSeverity(history)
      }))
    ];
  }


  private getKvRoleTags(member: MemberLookupDto): { label: string; severity: 'success' | 'danger' }[] {
    const role = (member.userRole ?? '').toLowerCase();
    if (role === 'manager') {
      return [{ label: "Зв'язковий", severity: 'danger' }];
    }

    if (role === 'mentor') {
      return [{ label: 'Впорядник', severity: 'success' }];
    }

    return [];
  }

  private getFullNameSortValue(member: Pick<MemberLookupDto, 'lastName' | 'firstName' | 'middleName'>): string {
    return `${member.lastName ?? ''} ${member.firstName ?? ''} ${member.middleName ?? ''}`.trim().toLowerCase();
  }

  private getMemberRoleSortWeight(member: Pick<MemberLookupDto, 'userRole' | 'leadershipHistories'>): number {
    const kvRoleWeight = this.getKvRoleSortWeight(member.userRole);
    const leadershipRoleWeight = (member.leadershipHistories ?? [])
      .filter(history => !history.endDate)
      .reduce(
        (lowest, history) => Math.min(lowest, getLeadershipRoleSortWeight(history.role)),
        Number.MAX_SAFE_INTEGER
      );

    return Math.min(kvRoleWeight, leadershipRoleWeight);
  }

  private getKvRoleSortWeight(role?: string | null): number {
    const normalized = (role ?? '').toLowerCase();
    if (normalized === 'manager') {
      return getLeadershipRoleSortWeight(LeadershipRole.Zvyazkovyi);
    }

    if (normalized === 'mentor') {
      return getLeadershipRoleSortWeight(LeadershipRole.Vykhovnyk);
    }

    return Number.MAX_SAFE_INTEGER;
  }

  private getMemberRoleLabel(history: LeadershipHistoryDto): string {
    const roleName = this.getRoleDisplayName(history.role);
    const type = (history.leadershipType ?? '').toLowerCase();
    if (type === 'group' && history.groupName) {
      return `${roleName}: ${history.groupName}`;
    }

    return roleName;
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
