
import { Component, OnChanges, inject, ChangeDetectionStrategy, input } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Observable, catchError, forkJoin, of, switchMap } from 'rxjs';
import { ButtonModule } from '@openng/optimus-ui/button';
import { DialogModule } from '@openng/optimus-ui/dialog';
import { MultiSelectModule } from '@openng/optimus-ui/multiselect';
import { SelectModule } from '@openng/optimus-ui/select';
import { TableModule } from '@openng/optimus-ui/table';
import { TagModule } from '@openng/optimus-ui/tag';
import { ToggleSwitchModule } from '@openng/optimus-ui/toggleswitch';
import { TooltipModule } from '@openng/optimus-ui/tooltip';
import { MemberLookupDto } from '../../models/requests/member/member-lookup.dto';
import { GroupDto } from '../../models/group.dto';
import { GroupService, MentorAssignmentDto } from '../../services/group-service/group.service';
import { MemberService } from '../../services/member-service/member.service';
import { PermissionService } from '../../../authModule/services/permission-service/permission.service';
import { AuthService } from '../../../authModule/services/auth-service/auth.service';
import { UserService } from '../../../adminModule/services/user-service/user.service';
import { parseUtcDateTime } from '../../../../shared/functions/utc-date-time.function';
import { LocalUtcDatePipe } from '../../../../shared/pipes/local-utc-date.pipe';
import { EmptyStateComponent } from '../../../../shared/empty-state/empty-state';
import { holdsOffice } from '../../functions/system-role.function';
import { toDateOnlyString } from '../../functions/to-date-only-string.function';
import { LeadershipService } from '../../services/leadership-service/leadership.service';
import { LeadershipRole } from '../../models/enums/leadership-role.enum';
import { memberDisplayName } from '../../functions/leadership-role-display.function';
import { LeadershipDto, LeadershipHistoryDto } from '../../models/requests/leadership/leadership.dto';

interface MentorAssignmentRow {
  mentor: MemberLookupDto;
  groups: GroupDto[];
  isManager?: boolean;
}

@Component({
  selector: 'app-kv-panel',
  imports: [
    FormsModule,
    ButtonModule,
    DialogModule,
    MultiSelectModule,
    SelectModule,
    TableModule,
    TagModule,
    ToggleSwitchModule,
    TooltipModule,
    LocalUtcDatePipe,
    EmptyStateComponent
],
  templateUrl: './kv-panel.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './kv-panel.css'
})
export class KvPanelComponent implements OnChanges {
  readonly kurinKey = input('');

  private readonly groupService = inject(GroupService);
  private readonly memberService = inject(MemberService);
  private readonly permissionService = inject(PermissionService);
  private readonly authService = inject(AuthService);
  private readonly userService = inject(UserService);
  private readonly leadershipService = inject(LeadershipService);
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
    if (this.kurinKey()) {
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
        label: memberDisplayName(candidate),
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
        label: memberDisplayName(candidate),
        value: candidate.userKey as string
      }));
  }

  // The table tracks rows by identity, so the Звʼязковий's row is built once per data change, not
  // on every check: a fresh object each time re-rendered that row between mousedown and click, and
  // its pencil never opened the dialog.
  private kvRowsCache: { manager: MemberLookupDto | null; mentorRows: MentorAssignmentRow[]; rows: MentorAssignmentRow[] } | null = null;

  get kvRows(): MentorAssignmentRow[] {
    if (this.kvRowsCache?.manager === this.manager && this.kvRowsCache.mentorRows === this.mentorRows) {
      return this.kvRowsCache.rows;
    }

    this.kvRowsCache = { manager: this.manager, mentorRows: this.mentorRows, rows: this.buildKvRows() };
    return this.kvRowsCache.rows;
  }

  private buildKvRows(): MentorAssignmentRow[] {
    if (!this.manager) {
      return this.mentorRows;
    }

    const manager = this.manager;
    const managerRow: MentorAssignmentRow = {
      mentor: manager,
      groups: this.mentorRows.find(row => this.isSameUserMember(row.mentor, manager))?.groups ?? [],
      isManager: true
    };

    return [managerRow, ...this.mentorRows.filter(row => !this.isSameUserMember(row.mentor, manager))];
  }

  loadData(): void {
    this.isLoading = true;

    forkJoin({
      groups: this.groupService.getAllByKurinKey(this.kurinKey()),
      kvMembers: this.memberService.getKVMembers(this.kurinKey()),
      mentorAssignments: this.groupService.getMentorAssignments(this.kurinKey()),
      mentorCandidates: this.canManageKv ? this.memberService.getMentorCandidates(this.kurinKey()) : of([] as MemberLookupDto[])
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
    if (!this.canManageKv) {
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
    const requests: Observable<unknown>[] = [
      ...toAssign.map(groupKey => this.groupService.assignMentor(groupKey, this.selectedMentorUserKey as string)),
      ...toRevoke.map(groupKey => this.groupService.revokeMentor(groupKey, this.selectedMentorUserKey as string))
    ];

    // Saved with no гуртки, someone who is not in the КВ yet becomes a впорядник without one.
    const newcomer = this.mentorCandidates.find(candidate => candidate.userKey === this.selectedMentorUserKey);
    if (selectedGroupKeys.size === 0 && newcomer && !this.isInKv(newcomer)) {
      requests.push(this.seatInKv(newcomer, LeadershipRole.Vykhovnyk));
    }

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

  /** Opens the КВ провід form, where інструктори and впорядники are seated with or without a гурток. */
  openKvLeadership(): void {
    if (!this.canManageKv) {
      return;
    }

    this.loadKvLeadership().subscribe(leadership => {
      if (leadership?.leadershipKey) {
        this.router.navigate(['/leadership', leadership.leadershipKey, 'kv', this.kurinKey()]);
        return;
      }

      this.router.navigate(['/leadership/create', 'kv', this.kurinKey()]);
    });
  }

  openTransferDialog(): void {
    if (!this.canManageKv) {
      return;
    }

    this.selectedManagerUserKey = null;
    this.transferDialogVisible = true;
  }

  // Transferring means moving the Зв'язковий office, not changing a system role: access is derived
  // from the офіс. The upsert endpoint diffs the провід, so sending the roster with the new holder
  // ends the outgoing one and seats the incoming one in a single call.
  transferManagerRole(): void {
    if (!this.selectedManagerUserKey || this.transferInProgress) {
      return;
    }

    const incoming = this.kvMembers.find(member => member.userKey === this.selectedManagerUserKey);
    if (!incoming) {
      return;
    }

    this.transferInProgress = true;
    this.leadershipService.getLeadershipByTypeAndKey('kv', this.kurinKey()).pipe(
      switchMap(leadership => {
        const today = toDateOnlyString(new Date()) ?? '';
        const retained = (leadership.leadershipHistories ?? [])
          .filter(history => !history.endDate && history.role !== LeadershipRole.Zvyazkovyi);

        return this.leadershipService.update(leadership.leadershipKey!, {
          ...leadership,
          leadershipHistories: [
            ...retained,
            {
              leadershipHistoryKey: '',
              leadershipKey: leadership.leadershipKey!,
              role: LeadershipRole.Zvyazkovyi,
              startDate: today,
              endDate: null,
              member: incoming
            }
          ]
        });
      })
    ).subscribe({
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
  getStatusLabel(member: MemberLookupDto): string {
    if (this.isUserRole(member, 'Manager')) {
      return "Зв'язковий";
    }

    if (this.isUserRole(member, 'Mentor')) {
      return 'Впорядник';
    }

    if (this.isUserRole(member, 'Instructor')) {
      return 'Інструктор';
    }

    return 'Учасник';
  }

  getStatusSeverity(member: MemberLookupDto): 'danger' | 'success' | 'info' | 'warn' {
    if (this.isUserRole(member, 'Manager')) {
      return 'danger';
    }

    if (this.isUserRole(member, 'Mentor')) {
      return 'success';
    }

    if (this.isUserRole(member, 'Instructor')) {
      return 'warn';
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

    // Інструктори and впорядники seated without a гурток are КВ too, so they get a row of their own.
    this.kvMembers
      .filter(member => this.isUserRole(member, 'Mentor') || this.isUserRole(member, 'Instructor'))
      .forEach(mentor => {
        const key = mentor.userKey ?? mentor.memberKey;
        if (!rowMap.has(key)) {
          rowMap.set(key, { mentor, groups: [] });
        }
      });

    this.mentorRows = [...rowMap.values()].sort((a, b) => memberDisplayName(a.mentor).localeCompare(memberDisplayName(b.mentor)));
  }

  get archivedAssignments(): MentorAssignmentDto[] {
    return this.mentorAssignments
      .filter(assignment => !!assignment.revokedAtUtc)
      .sort((a, b) =>
        (parseUtcDateTime(b.revokedAtUtc)?.getTime() ?? 0)
        - (parseUtcDateTime(a.revokedAtUtc)?.getTime() ?? 0)
      );
  }

  getAssignmentMemberName(assignment: MentorAssignmentDto): string {
    return assignment.member ? memberDisplayName(assignment.member) : 'Невідомий учасник';
  }

  private isInKv(member: MemberLookupDto): boolean {
    return this.kvRows.some(row => this.isSameUserMember(row.mentor, member));
  }

  private loadKvLeadership(): Observable<LeadershipDto | null> {
    return this.leadershipService.getLeadershipByTypeAndKey('kv', this.kurinKey()).pipe(
      catchError(() => of(null))
    );
  }

  // The КВ провід is diffed on save, so the roster goes back whole with the new seat added.
  private seatInKv(member: MemberLookupDto, role: LeadershipRole): Observable<LeadershipDto> {
    const today = toDateOnlyString(new Date()) ?? '';
    return this.loadKvLeadership().pipe(
      switchMap(leadership => {
        const seat: LeadershipHistoryDto = {
          leadershipHistoryKey: '',
          leadershipKey: leadership?.leadershipKey ?? '',
          role,
          startDate: today,
          endDate: null,
          member
        };
        const retained = (leadership?.leadershipHistories ?? []).filter(history => !history.endDate);

        if (!leadership?.leadershipKey) {
          return this.leadershipService.create({
            leadershipKey: null,
            type: 'kv',
            entityKey: this.kurinKey(),
            startDate: today,
            endDate: null,
            leadershipHistories: [seat]
          });
        }

        return this.leadershipService.update(leadership.leadershipKey, {
          ...leadership,
          leadershipHistories: [...retained, seat]
        });
      })
    );
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

  private isUserRole(member: MemberLookupDto, role: 'Manager' | 'Mentor' | 'Instructor'): boolean {
    if (role === 'Manager') {
      return holdsOffice(member.userRole, LeadershipRole.Zvyazkovyi);
    }

    if (role === 'Instructor') {
      return holdsOffice(member.userRole, LeadershipRole.Instruktor);
    }

    return holdsOffice(member.userRole, LeadershipRole.Hurtkoviy)
      || holdsOffice(member.userRole, LeadershipRole.Vykhovnyk);
  }

  private refreshCurrentUserRoleAfterTransfer(): void {
    const currentState = this.authService.getAuthStateValue();
    if (!currentState || currentState.userKey === this.selectedManagerUserKey || !this.permissionService.canManageWholeKurin()) {
      return;
    }

    // Permissions are re-derived from offices on the next token; refresh to pick them up.
    this.authService.refreshToken().subscribe({
      error: (err) => console.error('Error refreshing token after manager transfer:', err)
    });
  }

  /** Template adapter — the format lives in leadershipRoleDisplay.function.ts. */
  getMemberName(member: MemberLookupDto): string {
    return memberDisplayName(member);
  }
}
