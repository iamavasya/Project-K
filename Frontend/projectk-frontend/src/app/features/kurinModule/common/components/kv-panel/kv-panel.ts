import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { ButtonModule } from 'primeng/button';
import { DialogModule } from 'primeng/dialog';
import { MultiSelectModule } from 'primeng/multiselect';
import { SelectModule } from 'primeng/select';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { TooltipModule } from 'primeng/tooltip';
import { MemberLookupDto } from '../../models/requests/member/memberLookupDto';
import { GroupDto } from '../../models/groupDto';
import { GroupService, MentorAssignmentDto } from '../../services/group-service/group.service';
import { MemberService } from '../../services/member-service/member.service';
import { PermissionService } from '../../../../authModule/services/permission.service';
import { AuthService } from '../../../../authModule/services/authService/auth.service';
import { UserService } from '../../../../adminModule/services/user.service';

interface MentorAssignmentRow {
  mentor: MemberLookupDto;
  groups: GroupDto[];
  isManager?: boolean;
}

@Component({
  selector: 'app-kv-panel',
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    DialogModule,
    MultiSelectModule,
    SelectModule,
    TableModule,
    TagModule,
    ToggleSwitchModule,
    TooltipModule
  ],
  templateUrl: './kv-panel.html',
  styleUrl: './kv-panel.css'
})
export class KvPanelComponent implements OnChanges {
  @Input() kurinKey = '';

  private readonly groupService = inject(GroupService);
  private readonly memberService = inject(MemberService);
  private readonly permissionService = inject(PermissionService);
  private readonly authService = inject(AuthService);
  private readonly userService = inject(UserService);
  private readonly router = inject(Router);

  groups: GroupDto[] = [];
  kvMembers: MemberLookupDto[] = [];
  mentorCandidates: MemberLookupDto[] = [];
  mentorAssignments: MentorAssignmentDto[] = [];
  mentorRows: MentorAssignmentRow[] = [];
  manager: MemberLookupDto | null = null;
  isLoading = false;
  showArchived = false;

  assignmentDialogVisible = false;
  selectedMentorUserKey: string | null = null;
  selectedGroupKeys: string[] = [];
  assignmentSaveInProgress = false;

  transferDialogVisible = false;
  selectedManagerUserKey: string | null = null;
  transferInProgress = false;

  ngOnChanges(): void {
    if (this.kurinKey) {
      this.loadData();
    }
  }

  get canManageKv(): boolean {
    return this.permissionService.canManageMentors();
  }

  get mentorOptions(): { label: string; value: string }[] {
    return this.mentorCandidates
      .filter(candidate => !!candidate.userKey)
      .map(candidate => ({
        label: this.getMemberName(candidate),
        value: candidate.userKey as string
      }));
  }

  get groupOptions(): { label: string; value: string }[] {
    return this.groups.map(group => ({ label: group.name, value: group.groupKey }));
  }

  get managerTransferOptions(): { label: string; value: string }[] {
    return this.mentorCandidates
      .filter(candidate => !!candidate.userKey && candidate.userKey !== this.manager?.userKey)
      .map(candidate => ({
        label: this.getMemberName(candidate),
        value: candidate.userKey as string
      }));
  }

  get kvRows(): MentorAssignmentRow[] {
    const managerRow = this.manager
      ? [{ mentor: this.manager, groups: this.groups, isManager: true }]
      : [];

    return [...managerRow, ...this.mentorRows];
  }

  loadData(): void {
    this.isLoading = true;

    forkJoin({
      groups: this.groupService.getAllByKurinKey(this.kurinKey),
      kvMembers: this.memberService.getKVMembers(this.kurinKey),
      mentorAssignments: this.groupService.getMentorAssignments(this.kurinKey),
      mentorCandidates: this.canManageKv ? this.memberService.getMentorCandidates(this.kurinKey) : of([] as MemberLookupDto[])
    }).subscribe({
      next: ({ groups, kvMembers, mentorAssignments, mentorCandidates }) => {
        this.groups = groups;
        this.kvMembers = kvMembers;
        this.mentorAssignments = mentorAssignments;
        this.mentorCandidates = this.canManageKv ? mentorCandidates : kvMembers;
        this.manager = this.kvMembers.find(member => this.isUserRole(member, 'Manager')) ?? null;
        this.buildMentorRows(groups, mentorAssignments);
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error loading KV data:', err);
        this.isLoading = false;
      }
    });
  }

  openAssignmentDialog(row?: MentorAssignmentRow): void {
    if (!this.canManageKv || row?.isManager) {
      return;
    }

    this.selectedMentorUserKey = row?.mentor.userKey ?? null;
    this.selectedGroupKeys = row?.groups.map(group => group.groupKey) ?? [];
    this.assignmentDialogVisible = true;
  }

  saveAssignments(): void {
    if (!this.selectedMentorUserKey || this.assignmentSaveInProgress) {
      return;
    }

    const currentGroupKeys = new Set(this.getGroupsForUser(this.selectedMentorUserKey).map(group => group.groupKey));
    const selectedGroupKeys = new Set(this.selectedGroupKeys);
    const toAssign = [...selectedGroupKeys].filter(groupKey => !currentGroupKeys.has(groupKey));
    const toRevoke = [...currentGroupKeys].filter(groupKey => !selectedGroupKeys.has(groupKey));
    const requests = [
      ...toAssign.map(groupKey => this.groupService.assignMentor(groupKey, this.selectedMentorUserKey as string)),
      ...toRevoke.map(groupKey => this.groupService.revokeMentor(groupKey, this.selectedMentorUserKey as string))
    ];

    if (requests.length === 0) {
      this.assignmentDialogVisible = false;
      return;
    }

    this.assignmentSaveInProgress = true;
    forkJoin(requests).subscribe({
      next: () => {
        this.assignmentSaveInProgress = false;
        this.assignmentDialogVisible = false;
        this.loadData();
      },
      error: (err) => {
        console.error('Error saving mentor assignments:', err);
        this.assignmentSaveInProgress = false;
      }
    });
  }

  openTransferDialog(): void {
    if (!this.canManageKv) {
      return;
    }

    this.selectedManagerUserKey = null;
    this.transferDialogVisible = true;
  }

  transferManagerRole(): void {
    if (!this.selectedManagerUserKey || this.transferInProgress) {
      return;
    }

    this.transferInProgress = true;
    this.userService.changeUserRole(this.selectedManagerUserKey, 1).subscribe({
      next: () => {
        this.refreshCurrentUserRoleAfterTransfer();
        this.transferInProgress = false;
        this.transferDialogVisible = false;
        this.loadData();
      },
      error: (err) => {
        console.error('Error transferring manager role:', err);
        this.transferInProgress = false;
      }
    });
  }

  getMemberName(member: MemberLookupDto): string {
    return `${member.lastName} ${member.firstName}${member.middleName ? ` ${member.middleName}` : ''}`;
  }

  getStatusLabel(member: MemberLookupDto): string {
    if (this.isUserRole(member, 'Manager')) {
      return "Зв'язковий";
    }

    if (this.isUserRole(member, 'Mentor')) {
      return 'Впорядник';
    }

    return 'Учасник';
  }

  getStatusSeverity(member: MemberLookupDto): 'danger' | 'success' | 'info' {
    if (this.isUserRole(member, 'Manager')) {
      return 'danger';
    }

    if (this.isUserRole(member, 'Mentor')) {
      return 'success';
    }

    return 'info';
  }

  onMemberSelect(member: MemberLookupDto): void {
    if (member?.memberKey) {
      this.router.navigate(['/member', member.memberKey]);
    }
  }

  private buildMentorRows(groups: GroupDto[], assignments: MentorAssignmentDto[]): void {
    const groupMap = new Map(groups.map(group => [group.groupKey, group]));
    const rowMap = new Map<string, MentorAssignmentRow>();

    assignments
      .filter(assignment => !assignment.revokedAtUtc)
      .forEach(assignment => {
        if (!assignment.member) {
          return;
        }

        const group = groupMap.get(assignment.groupKey);
        if (!group) {
          return;
        }

        const enrichedMember = this.getEnrichedMember(assignment.member);
        const key = enrichedMember.userKey ?? enrichedMember.memberKey;
        const existing = rowMap.get(key) ?? { mentor: enrichedMember, groups: [] };
        existing.groups.push(group);
        rowMap.set(key, existing);
      });

    this.kvMembers
      .filter(member => this.isUserRole(member, 'Mentor'))
      .forEach(mentor => {
        const key = mentor.userKey ?? mentor.memberKey;
        if (!rowMap.has(key)) {
          rowMap.set(key, { mentor, groups: [] });
        }
      });

    this.mentorRows = [...rowMap.values()].sort((a, b) => this.getMemberName(a.mentor).localeCompare(this.getMemberName(b.mentor)));
  }

  get archivedAssignments(): MentorAssignmentDto[] {
    return this.mentorAssignments
      .filter(assignment => !!assignment.revokedAtUtc)
      .sort((a, b) => new Date(b.revokedAtUtc as string).getTime() - new Date(a.revokedAtUtc as string).getTime());
  }

  getAssignmentMemberName(assignment: MentorAssignmentDto): string {
    return assignment.member ? this.getMemberName(assignment.member) : 'Невідомий учасник';
  }

  private getGroupsForUser(userKey: string): GroupDto[] {
    return this.mentorRows.find(row => row.mentor.userKey === userKey)?.groups ?? [];
  }

  private getEnrichedMember(member: MemberLookupDto): MemberLookupDto {
    return this.kvMembers.find(candidate => this.isSameUserMember(candidate, member))
      ?? this.mentorCandidates.find(candidate => this.isSameUserMember(candidate, member))
      ?? member;
  }

  private isSameUserMember(left: MemberLookupDto, right: MemberLookupDto): boolean {
    if (left.userKey && right.userKey) {
      return left.userKey === right.userKey;
    }

    return left.memberKey === right.memberKey;
  }

  private isUserRole(member: MemberLookupDto, role: 'Manager' | 'Mentor'): boolean {
    return (member.userRole ?? '').toLowerCase() === role.toLowerCase();
  }

  private refreshCurrentUserRoleAfterTransfer(): void {
    const currentState = this.authService.getAuthStateValue();
    if (!currentState || currentState.userKey === this.selectedManagerUserKey || !this.permissionService.isManager()) {
      return;
    }

    this.authService.updateRole('Mentor');
    this.authService.refreshToken().subscribe({
      error: (err) => console.error('Error refreshing token after manager transfer:', err)
    });
  }
}
