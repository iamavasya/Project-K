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
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../authModule/services/authService/auth.service';
import { forkJoin, of } from 'rxjs';
import { EntityService } from '../../authModule/services/entity.service';
import { PermissionService } from '../../authModule/services/permission.service';
import { LeadershipPanelComponent } from '../common/components/leadership/leadership-panel/leadership-panel';
import { TextareaModule } from 'primeng/textarea';

@Component({
  selector: 'app-group-panel',
  imports: [TableModule, ButtonModule, GroupChevron, MemberList, DialogModule, MultiSelectModule, FormsModule, ReactiveFormsModule, TextareaModule, LeadershipPanelComponent],
  templateUrl: './group-panel.component.html',
  styleUrl: './group-panel.component.css'
})
export class GroupPanelComponent implements OnInit {

  private readonly route: ActivatedRoute = inject(ActivatedRoute);
  private readonly router: Router = inject(Router);
  private readonly memberService = inject(MemberService);
  private readonly groupService = inject(GroupService);
  private readonly authService = inject(AuthService);
  private readonly entityService = inject(EntityService);
  private readonly permissionService = inject(PermissionService);
  private readonly fb = inject(FormBuilder);
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
  canCreateMembers = false;
  canEditGroupProfile = false;
  profileEditMode = false;
  profileSaving = false;

  profileForm: FormGroup = this.fb.group({
    description: ['', Validators.maxLength(1000)]
  });


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
    return this.permissionService.canManageMentors();
  }

  get canManageMembers(): boolean {
    return this.canCreateMembers;
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
        this.patchProfileForm(group);
        if (this.canManageMentors) {
          this.loadMentorManagementData();
        }
      }
    });
    this.updateGroupAccess();
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

  startProfileEdit(): void {
    if (!this.group || !this.canEditGroupProfile) {
      return;
    }

    this.profileEditMode = true;
    this.patchProfileForm(this.group);
  }

  cancelProfileEdit(): void {
    this.profileEditMode = false;
    if (this.group) {
      this.patchProfileForm(this.group);
    }
  }

  saveProfile(): void {
    if (!this.group || this.profileForm.invalid) {
      return;
    }

    const raw = this.profileForm.value;
    const request = {
      name: this.group.name,
      description: this.normalizeText(raw.description)
    };

    this.profileSaving = true;
    this.groupService.update(this.groupKey, request).subscribe({
      next: (updated) => {
        this.group = updated;
        this.patchProfileForm(updated);
        this.profileEditMode = false;
        this.profileSaving = false;
      },
      error: (err) => {
        console.error('Error updating group profile:', err);
        this.profileSaving = false;
      }
    });
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

  private updateGroupAccess(): void {
    if (!this.groupKey) {
      this.canCreateMembers = false;
      this.canEditGroupProfile = false;
      return;
    }

    this.entityService.checkEntityAccess('group', this.groupKey, 'Create').subscribe({
      next: (canCreate) => {
        this.canCreateMembers = canCreate;
      },
      error: () => {
        this.canCreateMembers = false;
      }
    });

    this.entityService.checkEntityAccess('group', this.groupKey, 'Update').subscribe({
      next: (canUpdate) => {
        this.canEditGroupProfile = canUpdate;
      },
      error: () => {
        this.canEditGroupProfile = false;
      }
    });
  }

  private patchProfileForm(group: GroupDto): void {
    this.profileForm.patchValue({
      description: group.description ?? ''
    }, { emitEvent: false });
    this.profileForm.markAsPristine();
  }

  private normalizeText(value: unknown): string | null {
    const text = String(value ?? '').trim();
    return text.length > 0 ? text : null;
  }
}
