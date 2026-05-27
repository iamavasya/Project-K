import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { GroupService } from '../common/services/group-service/group.service';
import { GroupDto } from '../common/models/groupDto';
import { SplitButton } from 'primeng/splitbutton';
import { ManageAction, ManagePanel, ManagePanelConfig } from '../common/components/manage-panel/manage-panel';
import { MenuItem } from 'primeng/api';
import { MessageModule } from 'primeng/message';
import { TagModule } from 'primeng/tag';
import { TooltipModule } from 'primeng/tooltip';
import { KurinService } from '../common/services/kurin-service/kurin.service';
import { KurinNumberComponent } from '../common/components/kurin-number/kurin-number';
import { AuthService } from '../../authModule/services/authService/auth.service';
import { PermissionService } from '../../authModule/services/permission.service';
import { MemberList } from '../common/components/member-list/member-list';
import { KurinDto } from '../common/models/kurinDto';
import { OnboardingService, ZbtStats } from '../../authModule/services/onboarding.service';
import { KvPanelComponent } from '../common/components/kv-panel/kv-panel';
import { LeadershipPanelComponent } from '../common/components/leadership/leadership-panel/leadership-panel';

@Component({
  selector: 'app-kurin-panel',
  imports: [
    ReactiveFormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    SplitButton,
    ManagePanel,
    MessageModule,
    KurinNumberComponent,
    MemberList,
    TagModule,
    TooltipModule,
    KvPanelComponent,
    LeadershipPanelComponent
  ],
  templateUrl: './kurin-panel.component.html',
  styleUrls: ['./kurin-panel.component.css']
})
export class KurinPanelComponent implements OnInit {
  private readonly route: ActivatedRoute = inject(ActivatedRoute);
  private readonly router: Router = inject(Router);
  private readonly groupService = inject(GroupService);
  private readonly kurinService = inject(KurinService);
  private readonly authService = inject(AuthService);
  private readonly permissionService = inject(PermissionService);
  private readonly onboardingService = inject(OnboardingService);
  private readonly fb = inject(FormBuilder);

  groups: GroupDto[] = [];
  actions: MenuItem[] = [];

  kurinKey = '';
  kurinNumber: number | null = null;
  kurinData: KurinDto | null = null;
  zbtStats: ZbtStats | null = null;

  canManageGroups = false;
  canManageMembers = false;
  canEditKurinProfile = false;
  profileEditMode = false;
  profileSaving = false;
  descriptionExpanded = false;

  readonly descriptionCollapseLimit = 360;

  profileForm: FormGroup = this.fb.group({
    stanytsia: ['', Validators.maxLength(120)],
    regionOrCountry: ['', Validators.maxLength(120)],
    namedAfter: ['', Validators.maxLength(200)],
    description: ['', Validators.maxLength(4000)]
  });

  groupPanelConfig: ManagePanelConfig = {
    entityType: 'group',
    title: 'Гурток',
    fields: [
      {
        name: 'groupKey',
        label: 'Системний ключ',
        type: 'text',
        required: true,
        hiddenOn: ['create', 'delete'],
        disabledOn: ['update']
      },
      {
        name: 'kurinKey',
        label: 'Курінь',
        type: 'text',
        required: true,
        hiddenOn: ['create', 'delete', 'update'],
        disabledOn: ['update']
      },
      {
        name: 'name',
        label: 'Назва гуртка',
        type: 'text',
        placeholder: 'Наприклад: Сіроманці',
        required: true,
        hiddenOn: ['delete']
      }
    ],
    displayName: (entity: GroupDto) => entity.name,
    createFactory: () => ({ groupKey: '', name: '', kurinKey: this.kurinKey })
  };

  groupPanelVisible = false;
  groupPanelParameter: ManageAction | 'undef' = 'undef';
  selectedGroup: GroupDto | null = null;

  get descriptionText(): string {
    return this.kurinData?.description?.trim() ?? '';
  }

  get isDescriptionLong(): boolean {
    return this.descriptionText.length > this.descriptionCollapseLimit;
  }

  ngOnInit() {
    this.authService.getAuthState().subscribe(state => {
      if (state?.kurinKey) {
        this.kurinKey = state.kurinKey;
        this.canManageGroups = this.permissionService.canManageGroups();
        this.canManageMembers = this.permissionService.canManageGroups();
        this.canEditKurinProfile = this.permissionService.canManageGroups();
        this.refreshData();
      }
    });
  }

  refreshData() {
    if (!this.kurinKey) return;

    this.groupService.getAllByKurinKey(this.kurinKey).subscribe({
      next: (groups) => {
        this.groups = groups;
      },
      error: (error) => {
        console.error('Error fetching groups:', error);
      }
    });

    this.kurinService.getByKey(this.kurinKey).subscribe({
      next: (kurin) => {
        this.kurinNumber = kurin.number;
        this.kurinData = kurin;
        this.descriptionExpanded = false;
        this.patchProfileForm(kurin);
      },
      error: (error) => {
        console.error('Error fetching kurin:', error);
      }
    });

    if (!this.permissionService.isAdmin()) {
      this.zbtStats = null;
      return;
    }

    this.onboardingService.getOnboardingStats(this.kurinKey).subscribe({
      next: (stats) => {
        this.zbtStats = stats;
      },
      error: (error) => {
        console.error('Error fetching stats:', error);
      }
    });
  }

  prepareItemActions(item: GroupDto): void {
    this.actions = [
      {
        label: 'Редагувати',
        icon: 'pi pi-pencil',
        command: () => { this.onGroupActionClick(item, 'update'); }
      },
      {
        label: 'Видалити',
        icon: 'pi pi-trash',
        command: () => { this.onGroupActionClick(item, 'delete'); }
      }
    ];
  }

  onGroupActionClick(item: GroupDto | null, action: ManageAction) {
    this.groupPanelParameter = action;
    this.selectedGroup = action === 'create' ? null : item;
    this.groupPanelVisible = true;
  }

  onGroupManage(e: { action: ManageAction; entity: GroupDto }) {
    switch (e.action) {
      case 'create': this.groupService.create(e.entity).subscribe(() => this.refreshData()); break;
      case 'update': this.groupService.update(e.entity.groupKey, e.entity).subscribe(() => this.refreshData()); break;
      case 'delete': this.groupService.delete(e.entity.groupKey).subscribe(() => this.refreshData()); break;
    }
  }

  onOpenClick(groupKey: string): void {
    this.router.navigate(['/group', groupKey]);
  }

  onMemberCreate(): void {
    this.router.navigate(['/kurin', this.kurinKey, 'member', 'upsert']);
  }

  startProfileEdit(): void {
    if (!this.kurinData || !this.canEditKurinProfile) return;
    this.profileEditMode = true;
    this.patchProfileForm(this.kurinData);
  }

  cancelProfileEdit(): void {
    this.profileEditMode = false;
    if (this.kurinData) {
      this.patchProfileForm(this.kurinData);
    }
  }

  toggleDescription(): void {
    this.descriptionExpanded = !this.descriptionExpanded;
  }

  saveProfile(): void {
    if (!this.kurinData || this.profileForm.invalid) return;

    const raw = this.profileForm.value;
    const request: KurinDto = {
      ...this.kurinData,
      stanytsia: this.normalizeText(raw.stanytsia),
      regionOrCountry: this.normalizeText(raw.regionOrCountry),
      namedAfter: this.normalizeText(raw.namedAfter),
      description: this.normalizeText(raw.description)
    };

    this.profileSaving = true;
    this.kurinService.updateKurin(request).subscribe({
      next: (updated) => {
        this.kurinData = updated;
        this.kurinNumber = updated.number;
        this.descriptionExpanded = false;
        this.patchProfileForm(updated);
        this.profileEditMode = false;
        this.profileSaving = false;
      },
      error: (error) => {
        console.error('Error updating kurin profile:', error);
        this.profileSaving = false;
      }
    });
  }

  private patchProfileForm(kurin: KurinDto): void {
    this.profileForm.patchValue({
      stanytsia: kurin.stanytsia ?? '',
      regionOrCountry: kurin.regionOrCountry ?? '',
      namedAfter: kurin.namedAfter ?? '',
      description: kurin.description ?? ''
    }, { emitEvent: false });
    this.profileForm.markAsPristine();
  }

  private normalizeText(value: unknown): string | null {
    const text = String(value ?? '').trim();
    return text.length > 0 ? text : null;
  }
}
