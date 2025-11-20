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
import { ToggleSwitchModule } from 'primeng/toggleswitch';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';
import { FormsModule } from '@angular/forms';

import { LeadershipRole } from '../../../models/enums/leadership-role.enum';
import { toDateOnlyString } from '../../../functions/toDateOnlyString.function';
import { ROLE_DISPLAY_NAMES } from '../../../models/roleDisplayName';

const COMMON_ROLES: LeadershipRole[] = [
  LeadershipRole.Suddya, LeadershipRole.Skarbnyk, LeadershipRole.Pysar,
  LeadershipRole.Gospodar, LeadershipRole.Hronikar, LeadershipRole.Horunjiy
];
const MULTI_MEMBER_ROLES: LeadershipRole[] = [ LeadershipRole.Vykhovnyk, LeadershipRole.Instruktor ];

export const LEADERSHIP_ROLE_MAP: Record<string, LeadershipRole[]> = {
  kv: [ LeadershipRole.Zvyazkovyi, LeadershipRole.Vykhovnyk, LeadershipRole.Instruktor ],
  kurin: [ LeadershipRole.Kurinnuy, ...COMMON_ROLES ],
  group: [ LeadershipRole.Hurtkoviy, ...COMMON_ROLES ]
};

type LeadershipType = 'kurin' | 'group' | 'kv';

@Component({
  selector: 'app-leadership-component',
  imports: [
    CommonModule, ReactiveFormsModule, FormsModule,
    TableModule, DatePickerModule, ButtonModule, SelectModule, 
    InputTextModule, TooltipModule, ProgressSpinnerModule,
    ToggleSwitchModule, IconFieldModule, InputIconModule
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
  
  // Стан фільтрів
  showArchived = false;
  searchTerm = '';

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
      const rawType = params.get('type');
      this.leadershipType = rawType ? rawType.toLowerCase() as LeadershipType : null;
      this.entityKey = params.get('entityKey');

      if (this.leadershipKey) {
        this.loadData(this.leadershipKey);
      } else if (this.leadershipType && this.entityKey) {
        this.loadAllMembers(); 
        this.buildFormRowsFromDefaults(this.leadershipType);
        this.isLoading = false;
      }
    });
  }

  loadData(key: string): void {
    this.leadershipService.getLeadershipByKey(key).subscribe({
      next: (leadership) => {
        this.leadershipType = leadership.type!.toLowerCase() as LeadershipType;
        this.entityKey = leadership.entityKey!;
        this.loadAllMembers(); 
        this.patchForm(leadership);
        this.isLoading = false;
      },
      error: (err) => console.error(err)
    });
  }

  loadAllMembers(): void {
    if (!this.leadershipType) return;
    // ... (Логіка завантаження мемберів без змін)
    let groupKey: string | undefined = undefined;
    let kurinKey: string | undefined = undefined;
    const type = this.leadershipType.toLowerCase();

    if (type === 'group') groupKey = this.entityKey ?? undefined;
    else if (type === 'kurin' || type === 'kv') kurinKey = this.entityKey ?? undefined;

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
      next: (lookupMembers) => this.allMembers = lookupMembers,
      error: (err) => console.error(err)
    });
  }
  
  patchForm(data: LeadershipDto): void {
    this.leadershipForm.patchValue({
      startDate: data.startDate ? new Date(data.startDate) : null,
      endDate: data.endDate ? new Date(data.endDate) : null
    });

    this.leadershipHistories.clear();

    const typeKey = this.leadershipType?.toLowerCase() || '';
    const rolesForType = LEADERSHIP_ROLE_MAP[typeKey] || [];
    const rolesFromData = new Map<LeadershipRole, LeadershipHistoryDto[]>();

    // Групуємо дані
    data.leadershipHistories.forEach(history => {
      const role = history.role as LeadershipRole;
      if (!rolesFromData.has(role)) rolesFromData.set(role, []);
      rolesFromData.get(role)!.push(history);
    });

    rolesForType.forEach(role => {
      const histories = rolesFromData.get(role) || [];
      
      // Сортуємо: Архівні зверху, активні знизу
      histories.sort((a, b) => {
        if (a.endDate && !b.endDate) return -1;
        if (!a.endDate && b.endDate) return 1;
        return new Date(a.startDate).getTime() - new Date(b.startDate).getTime();
      });

      histories.forEach(h => {
        this.leadershipHistories.push(this.createHistoryRow(role, h));
      });

      // Якщо немає активного (без дати кінця) - додаємо пустий рядок для вводу
      const hasActiveMember = histories.some(h => !h.endDate);
      if (!hasActiveMember) {
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
    const isArchived = !!data?.endDate;
    return this.fb.group({
      role: [{ value: role, disabled: true }, Validators.required],
      member: [{ value: data?.member || null, disabled: isArchived }, Validators.required], 
      startDate: [data?.startDate ? new Date(data.startDate) : null, Validators.required],
      endDate: [data?.endDate ? new Date(data.endDate) : null],
      leadershipHistoryKey: [data?.leadershipHistoryKey || null],
      leadershipKey: [data?.leadershipKey || this.leadershipKey || null]
    });
  }

  // --- UI ФІЛЬТРАЦІЯ ---
  // Ця функція викликається в HTML для кожного рядка
  isRowVisible(index: number): boolean {
    const control = this.leadershipHistories.at(index);
    const val = control.getRawValue();
    const isArchived = !!val.endDate;

    // 1. Фільтр архіву
    if (!this.showArchived && isArchived) {
        return false; // Ховаємо
    }

    // 2. Пошук
    if (this.searchTerm) {
        const term = this.searchTerm.toLowerCase();
        const roleName = this.getRoleDisplayName(val.role).toLowerCase();
        const memberName = val.member 
            ? `${val.member.lastName} ${val.member.firstName}`.toLowerCase() 
            : '';
        
        // Якщо не знайшли ні в ролі, ні в імені - ховаємо
        if (!roleName.includes(term) && !memberName.includes(term)) {
            return false;
        }
    }

    return true; // Показуємо
  }

  // --- ВИДАЛЕННЯ ---
  onRemoveRow(index: number): void {
    const control = this.leadershipHistories.at(index);
    const role = control.getRawValue().role as LeadershipRole;

    // 1. Видаляємо рядок
    this.leadershipHistories.removeAt(index);

    // 2. Якщо це була обов'язкова роль і ми видалили єдиний запис, додаємо пустий
    if (!this.canHaveMultipleMembers(role)) {
       const remainingRows = this.getRoleRowsCount(role);
       if (remainingRows === 0) {
           // Додаємо назад пустий, щоб роль не зникла зі списку
           this.addRoleRow(role);
       }
    }
  }

  saveCadence(): void {
    if (this.leadershipForm.invalid) {
      this.leadershipForm.markAllAsTouched();
      return;
    }
    this.isLoading = true;
    const formOutput = this.leadershipForm.getRawValue();

    const payload: LeadershipDto = {
      leadershipKey: this.leadershipKey,
      type: (this.leadershipType![0].toUpperCase() + this.leadershipType?.slice(1)) as 'kurin' | 'group' | 'kv', 
      entityKey: this.entityKey!,
      startDate: toDateOnlyString(formOutput.startDate)!,
      endDate: toDateOnlyString(formOutput.endDate),
      
      // Відправляємо тільки заповнені рядки. Бекенд видалить ті, що ми прибрали.
      leadershipHistories: formOutput.leadershipHistories
        .filter((h: LeadershipHistoryDto) => h.member !== null) 
        .map((h: LeadershipHistoryDto) => ({
          ...h,
          startDate: toDateOnlyString(h.startDate)!,
          endDate: toDateOnlyString(h.endDate),
          member: {
            memberKey: h.member.memberKey,
            firstName: h.member.firstName,
            lastName: h.member.lastName,
            middleName: h.member.middleName
          }
        }))
    };

    const saveOperation = this.leadershipKey 
      ? this.leadershipService.update(this.leadershipKey, payload)
      : this.leadershipService.create(payload);

    saveOperation.subscribe({
      next: (response) => {
        this.isLoading = false;
        this.patchForm(response);
        const type = this.leadershipType?.toLowerCase();
        if (type == 'kurin' || type == 'kv') this.router.navigate(['/kurin']);
        else if (type == 'group') this.router.navigate(['/group', this.entityKey]);
      },
      error: (err) => {
        console.error(err);
        this.isLoading = false;
      }
    });
  }

  canHaveMultipleMembers(role: LeadershipRole): boolean {
    return MULTI_MEMBER_ROLES.includes(role);
  }

  addRoleRow(role: LeadershipRole): void {
    this.leadershipHistories.push(this.createHistoryRow(role));
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