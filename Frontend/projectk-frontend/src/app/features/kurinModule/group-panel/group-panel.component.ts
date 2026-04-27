import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { MemberService } from '../common/services/member-service/member.service';
import { MemberDto } from '../common/models/memberDto';
import { ButtonModule } from 'primeng/button';
import { GroupService } from '../common/services/group-service/group.service';
import { GroupChevron } from "../common/components/group-chevron/group-chevron";
import { GroupDto } from '../common/models/groupDto';
import { MemberList } from '../common/components/member-list/member-list';
import { MemberLookupDto } from '../common/models/requests/member/memberLookupDto';
import { DialogModule } from 'primeng/dialog';
import { MultiSelectModule } from 'primeng/multiselect';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../authModule/services/authService/auth.service';
import { forkJoin, of } from 'rxjs';

@Component({
  selector: 'app-group-panel',
  imports: [TableModule, ButtonModule, GroupChevron, MemberList, DialogModule, MultiSelectModule, FormsModule],
  templateUrl: './group-panel.component.html',
  styleUrl: './group-panel.component.css'
})
export class GroupPanelComponent implements OnInit {

  private readonly route: ActivatedRoute = inject(ActivatedRoute);
  private readonly router: Router = inject(Router);
  private readonly memberService = inject(MemberService);
  private readonly groupService = inject(GroupService);
  private readonly authService = inject(AuthService);
  groupKey = '';
  group: GroupDto | null = null;
  members: MemberDto[] = [];
  selectedMember: MemberDto | null = null;
  mentorDialogVisible = false;
  mentorCandidates: MemberLookupDto[] = [];
  assignedMentors: MemberLookupDto[] = [];
  selectedMentorUserKeys: string[] = [];
  initialMentorUserKeys: string[] = [];
  mentorSaveInProgress = false;


  tableHeaders: string[] = [
    'MemberKey',
    'FirstName',
    'LastName',
  ];

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.groupKey = params.get('groupKey')!;
    });
    this.refreshData();
  }

  get canManageMentors(): boolean {
    const role = this.authService.getAuthStateValue()?.role;
    return role === 'Manager' || role === 'Admin';
  }

  get canManageMembers(): boolean {
    const role = this.authService.getAuthStateValue()?.role?.trim().toLowerCase();
    return role !== 'user';
  }

  get mentorOptions(): { label: string; value: string; }[] {
    return this.mentorCandidates
      .filter(candidate => !!candidate.userKey)
      .map(candidate => ({
        label: `${candidate.lastName} ${candidate.firstName}${candidate.middleName ? ` ${candidate.middleName}` : ''}`,
        value: candidate.userKey as string
      }));
  }

  refreshData(): void {
    this.groupService.exists(this.groupKey).subscribe({
      next: (exists) => {
        if (!exists) {
          this.router.navigate(['/panel'], { replaceUrl: true });
        }
      }
    });
    this.groupService.getByKey(this.groupKey).subscribe({
      next: (group) => {
        this.group = group;
        if (this.canManageMentors) {
          this.loadMentorManagementData();
        }
      }
    });
    this.memberService.getAll(this.groupKey).subscribe({
      next: (members) => {
        this.members = members;
      },
      error: (err) => {
        console.error('Error fetching members:', err);
      }
    });
  }

  onMemberSelect() {
    this.router.navigate(['/member', this.selectedMember?.memberKey]);
  }

  onMemberCreate() {
    this.router.navigate(['/group', this.groupKey, 'member', 'upsert']);
  }

  openMentorDialog(): void {
    if (!this.canManageMentors) {
      return;
    }

    this.loadMentorManagementData();
    this.mentorDialogVisible = true;
  }

  private loadMentorManagementData(): void {
    if (!this.group?.kurinKey) {
      return;
    }

    forkJoin({
      mentorCandidates: this.memberService.getMentorCandidates(this.group.kurinKey),
      assigned: this.groupService.getMentors(this.groupKey)
    }).subscribe({
      next: ({ mentorCandidates, assigned }) => {
        this.mentorCandidates = mentorCandidates;
        this.assignedMentors = assigned;
        const assignedUserKeys = assigned
          .map(m => m.userKey)
          .filter((key): key is string => !!key);

        this.initialMentorUserKeys = [...assignedUserKeys];
        this.selectedMentorUserKeys = [...assignedUserKeys];
      },
      error: (err) => {
        console.error('Error loading mentor management data:', err);
      }
    });
  }

  saveMentorAssignments(): void {
    if (this.mentorSaveInProgress) {
      return;
    }

    const initial = new Set(this.initialMentorUserKeys);
    const selected = new Set(this.selectedMentorUserKeys);

    const toAssign = [...selected].filter(key => !initial.has(key));
    const toRevoke = [...initial].filter(key => !selected.has(key));

    if (toAssign.length === 0 && toRevoke.length === 0) {
      this.mentorDialogVisible = false;
      return;
    }

    const assignRequests = toAssign.map(userKey => this.groupService.assignMentor(this.groupKey, userKey));
    const revokeRequests = toRevoke.map(userKey => this.groupService.revokeMentor(this.groupKey, userKey));
    const requests = [...assignRequests, ...revokeRequests];

    this.mentorSaveInProgress = true;
    (requests.length ? forkJoin(requests) : of([])).subscribe({
      next: () => {
        this.mentorSaveInProgress = false;
        this.mentorDialogVisible = false;
        this.loadMentorManagementData();
      },
      error: (err) => {
        console.error('Error saving mentor assignments:', err);
        this.mentorSaveInProgress = false;
      }
    });
  }
}
