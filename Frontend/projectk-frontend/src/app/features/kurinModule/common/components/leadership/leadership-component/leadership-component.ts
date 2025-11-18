import { Component, inject, OnInit } from '@angular/core';
import { LeadershipDto, LeadershipHistoryDto } from '../../../models/requests/leadership/leadershipDto';
import { MemberLookupDto } from '../../../models/requests/member/memberLookupDto';
import { MemberDto } from '../../../models/memberDto';
import { ActivatedRoute, Router } from '@angular/router';
import { LeadershipService } from '../../../services/leadership-service/leadership-service';
import { MemberService } from '../../../services/member-service/member.service';
import { FormArray, FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { map } from 'rxjs/operators';

import { TableModule } from 'primeng/table';
import { CommonModule } from '@angular/common';
import { DatePickerModule } from 'primeng/datepicker';
import { ButtonModule } from "primeng/button";
import { InputTextModule } from 'primeng/inputtext';
import { TooltipModule } from 'primeng/tooltip';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { SelectModule } from 'primeng/select';
import { LeadershipRole } from '../../../models/enums/leadership-role.enum';
import { toDateOnlyString } from '../../../functions/toDateOnlyString.function';

export const ROLE_DISPLAY_NAMES: Record<LeadershipRole, string> = {
    [LeadershipRole.Kurinnuy]:   'Курінний',
    [LeadershipRole.Hurtkoviy]:  'Гуртковий',
    [LeadershipRole.Suddya]:     'Суддя',
    [LeadershipRole.Pysar]:      'Писар',
    [LeadershipRole.Skarbnyk]:   'Скарбник',
    [LeadershipRole.Horunjiy]:   'Хорунжий',
    [LeadershipRole.Gospodar]:   'Господар',
    [LeadershipRole.Hronikar]:   'Хронікар',
    [LeadershipRole.Instruktor]: 'Інструктор',
    [LeadershipRole.Zvyazkovyi]: 'Зв\'язковий',
    [LeadershipRole.Vykhovnyk]: 'Впорядник',
};

const COMMON_ROLES: LeadershipRole[] = [
    LeadershipRole.Suddya,
    LeadershipRole.Skarbnyk,
    LeadershipRole.Pysar,
    LeadershipRole.Gospodar,
    LeadershipRole.Hronikar,
    LeadershipRole.Horunjiy
];
const MULTI_MEMBER_ROLES: LeadershipRole[] = [ LeadershipRole.Vykhovnyk, LeadershipRole.Instruktor ]; // Roles that can have multiple members

export const LEADERSHIP_ROLE_MAP: Record<string, LeadershipRole[]> = {
    kv: [
        LeadershipRole.Zvyazkovyi,
        LeadershipRole.Vykhovnyk,
        LeadershipRole.Instruktor
    ],
    kurin: [
        LeadershipRole.Kurinnuy,
        ...COMMON_ROLES
    ],
    group: [
        LeadershipRole.Hurtkoviy,
        ...COMMON_ROLES
    ]
};

type LeadershipType = 'kurin' | 'group' | 'kv';

@Component({
  selector: 'app-leadership-component',
  imports: [
    CommonModule, ReactiveFormsModule, TableModule, DatePickerModule,
    ButtonModule, SelectModule, InputTextModule, TooltipModule,
    ProgressSpinnerModule
  ],
  templateUrl: './leadership-component.html',
  styleUrl: './leadership-component.css'
})
export class LeadershipComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly leadershipService = inject(LeadershipService);
  private readonly memberService = inject(MemberService);
  private readonly fb = inject(FormBuilder);

  leadershipForm: FormGroup;
  leadershipKey: string | null = null;
  leadershipType: LeadershipType | null = null;
  entityKey: string | null = null;
  isLoading = false;
  
  allMembers: MemberLookupDto[] = [];
  filteredMembers: MemberLookupDto[] = [];

  constructor() {
    this.leadershipForm = this.fb.group({
      startDate: [null, Validators.required],
      endDate: [null],
      leadershipHistories: this.fb.array([])
    });
  }

  ngOnInit(): void {
    this.isLoading = true;

    this.route.paramMap.subscribe(params => {
      this.leadershipKey = params.get('leadershipKey');
      this.leadershipType = params.get('type') as LeadershipType;
      this.entityKey = params.get('entityKey');

      if (this.leadershipKey) {
        this.loadData(this.leadershipKey);
      } else if (this.leadershipType && this.entityKey) {
        this.loadAllMembers(); 
        this.buildFormRowsFromDefaults(this.leadershipType);
        this.isLoading = false;
      } else {
        console.error('Missing route parameters');
        this.isLoading = false;
      }
    });
  }

  loadData(key: string): void {
    this.leadershipService.getLeadershipByKey(key).subscribe({
      next: (leadership) => {
        this.leadershipType = leadership.type!; 
        this.entityKey = leadership.entityKey!;

        this.loadAllMembers(); 
        
        this.patchForm(leadership);
        this.isLoading = false;
      },
      error: (err) => {
        // ...
      }
    });
  }

  loadAllMembers(): void {
    if (!this.leadershipType) return;

    let groupKey: string | undefined = undefined;
    let kurinKey: string | undefined = undefined;

    if (this.leadershipType === 'group') {
      groupKey = this.entityKey ?? undefined;
    } else if (this.leadershipType === 'kurin' || this.leadershipType === 'kv') {
      kurinKey = this.entityKey ?? undefined;
    }

    this.memberService.getAll(groupKey, kurinKey).pipe(
      map((fullMembers: MemberDto[]) => 
        fullMembers.map(member => ({
          memberKey: member.memberKey,
          firstName: member.firstName,
          middleName: member.middleName,
          lastName: member.lastName
        }))
      )
    ).subscribe({
      next: (lookupMembers) => {
        this.allMembers = lookupMembers;
      },
      error: (err) => {
        console.error('Failed to load members for lookup', err);
      }
    });
  }
  
  patchForm(data: LeadershipDto): void {
    this.leadershipForm.patchValue({
      startDate: data.startDate ? toDateOnlyString(data.startDate) : null,
      endDate: data.endDate ? toDateOnlyString(data.endDate) : null
    });

    this.leadershipHistories.clear();

    const rolesForType = LEADERSHIP_ROLE_MAP[this.leadershipType!] || [];
    const rolesFromData = new Map<LeadershipRole, LeadershipHistoryDto[]>();

    // Group data by role
    data.leadershipHistories.forEach(history => {
      const role = history.role as LeadershipRole;
      if (!rolesFromData.has(role)) {
        rolesFromData.set(role, []);
      }
      rolesFromData.get(role)!.push(history);
    });

    // Add rows for roles that have data
    data.leadershipHistories.forEach(history => {
      const role = history.role as LeadershipRole;
      this.leadershipHistories.push(this.createHistoryRow(role, history));
    });

    // Add empty rows for roles without data
    rolesForType.forEach(role => {
      if (!rolesFromData.has(role)) {
        this.leadershipHistories.push(this.createHistoryRow(role));
      }
    });
  }

  buildFormRowsFromDefaults(type: LeadershipType): void {
    const roles = LEADERSHIP_ROLE_MAP[type] || [];
    roles.forEach(role => {
      this.leadershipHistories.push(this.createHistoryRow(role));
    });
  }

  get leadershipHistories(): FormArray {
    return this.leadershipForm.get('leadershipHistories') as FormArray;
  }

  private createHistoryRow(role: LeadershipRole, data: LeadershipHistoryDto | null = null): FormGroup {
    return this.fb.group({
      role: [{ value: role, disabled: true }, Validators.required],
      member: [data?.member || null, Validators.required], 
      startDate: [data?.startDate ? toDateOnlyString(data.startDate) : null, Validators.required],
      endDate: [data?.endDate ? toDateOnlyString(data.endDate) : null],
      leadershipHistoryKey: [data?.leadershipHistoryKey || null],
      leadershipKey: [data?.leadershipKey || this.leadershipKey || null]
    });
  }

  getMemberFullName(member: MemberLookupDto): string {
    if (!member) return '';
    return `${member.lastName} ${member.firstName} ${member.middleName || ''}`;
  }

  closeEntireCadence(): void {
    const today = new Date();
    this.leadershipForm.get('endDate')?.setValue(today);
    
    this.leadershipHistories.controls.forEach(control => {
      if (!control.get('endDate')?.value) {
        control.get('endDate')?.setValue(today);
      }
    });
  }

  saveCadence(): void {
    if (this.leadershipForm.invalid) {
      this.leadershipForm.markAllAsTouched();
      this.leadershipHistories.markAllAsTouched();
      console.error('Form is invalid');
      return;
    }

    this.isLoading = true;
    const formOutput = this.leadershipForm.getRawValue();

    const payload: LeadershipDto = {
      leadershipKey: this.leadershipKey,
      type: this.leadershipType!,
      entityKey: this.entityKey!,
      startDate: toDateOnlyString(formOutput.startDate)!,
      endDate: toDateOnlyString(formOutput.endDate),
      
      leadershipHistories: formOutput.leadershipHistories
        .filter((h: any) => h.member !== null)
        .map((h: any) => ({
          ...h,
          startDate: toDateOnlyString(h.startDate)!,
          endDate: toDateOnlyString(h.endDate),
          member: {
            memberKey: h.member.memberKey,
            firstName: h.member.firstName,
            middleName: h.member.middleName,
            lastName: h.member.lastName,
          }
        }))
    };

    const saveOperation = this.leadershipKey 
      ? this.leadershipService.update(this.leadershipKey, payload)
      : this.leadershipService.create(payload);

    saveOperation.subscribe({
      next: (response) => {
        console.log('Saved successfully!', response);
        this.isLoading = false;
        this.patchForm(response); 
      },
      error: (err) => {
        console.error('Failed to save', err);
        this.isLoading = false;
      }
    });
  }

  canHaveMultipleMembers(role: LeadershipRole): boolean {
    return MULTI_MEMBER_ROLES.includes(role);
  }

  addRoleRow(role: LeadershipRole): void {
    if (!this.canHaveMultipleMembers(role)) {
      console.warn(`Role ${role} cannot have multiple members`);
      return;
    }
    this.leadershipHistories.push(this.createHistoryRow(role));
  }

  removeRoleRow(index: number): void {
    const role = this.leadershipHistories.at(index).getRawValue().role as LeadershipRole;
    
    if (!this.canHaveMultipleMembers(role)) {
      console.warn(`Cannot remove the only entry for role ${role}`);
      return;
    }

    // Check if there's at least one more row with the same role
    const sameRoleCount = this.leadershipHistories.controls.filter(
      (control, i) => i !== index && control.getRawValue().role === role
    ).length;

    if (sameRoleCount > 0) {
      this.leadershipHistories.removeAt(index);
    } else {
      console.warn(`Cannot remove the last entry for role ${role}`);
    }
  }

  getRoleRowsCount(role: LeadershipRole): number {
    return this.leadershipHistories.controls.filter(
      control => control.getRawValue().role === role
    ).length;
  }

  public getRoleDisplayName(role: LeadershipRole): string {
        return ROLE_DISPLAY_NAMES[role] || role;
  }
}