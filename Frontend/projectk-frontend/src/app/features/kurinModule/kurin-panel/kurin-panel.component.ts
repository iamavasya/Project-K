import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpResponse } from '@angular/common/http';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { InputTextModule } from 'primeng/inputtext';
import { TextareaModule } from 'primeng/textarea';
import { GroupService } from '../common/services/group-service/group.service';
import { GroupDto } from '../common/models/groupDto';
import { SplitButton } from 'primeng/splitbutton';
import { ManageAction, ManagePanel, ManagePanelConfig } from '../common/components/manage-panel/manage-panel';
import { MenuItem } from 'primeng/api';
import { MenuModule } from 'primeng/menu';
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
import { MenuItemsCache } from '../common/functions/menuItemsCache';

@Component({
  selector: 'app-kurin-panel',
  imports: [
    ReactiveFormsModule,
    TableModule,
    ButtonModule,
    InputTextModule,
    TextareaModule,
    SplitButton,
    MenuModule,
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
  canManageKurinSettings = false;
  profileEditMode = false;
  profileSaving = false;
  reportDownloading = false;
  descriptionExpanded = false;

  readonly descriptionCollapseLimit = 360;
  private readonly kurinEditMenuCache = new MenuItemsCache();

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

  get hasKurinEditActions(): boolean {
    return this.canEditKurinProfile || this.canManageGroups || this.canManageKurinSettings;
  }

  get kurinEditMenuItems(): MenuItem[] {
    return this.kurinEditMenuCache.get(
      [
        this.canEditKurinProfile,
        this.canManageGroups,
        this.canManageKurinSettings,
        this.reportDownloading,
        this.kurinKey
      ],
      () => this.buildKurinEditMenuItems());
  }

  private buildKurinEditMenuItems(): MenuItem[] {
    const items: MenuItem[] = [];

    if (this.canEditKurinProfile) {
      items.push({
        label: 'Редагування даних куреня',
        icon: 'pi pi-pencil',
        command: () => this.startProfileEdit()
      });
    }

    if (this.canManageGroups) {
      items.push({
        label: 'Експорт звіту куреня',
        icon: 'pi pi-file-pdf',
        disabled: this.reportDownloading || !this.kurinKey,
        command: () => this.downloadReportPdf()
      });
    }

    if (this.canManageKurinSettings) {
      items.push({
        label: 'Налаштування куреня',
        icon: 'pi pi-cog',
        disabled: !this.kurinKey,
        command: () => this.openKurinSettings()
      });
    }

    return items;
  }

  ngOnInit() {
    this.authService.getAuthState().subscribe(state => {
      if (state?.kurinKey) {
        this.kurinKey = state.kurinKey;
        this.canManageGroups = this.permissionService.canManageGroups();
        this.canManageMembers = this.permissionService.canManageGroups();
        this.canEditKurinProfile = this.permissionService.canManageGroups();
        this.canManageKurinSettings = this.permissionService.canManageKurinSettings();
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

  openKurinSettings(): void {
    if (!this.kurinKey || !this.canManageKurinSettings) return;
    this.router.navigate(['/kurin', this.kurinKey, 'settings']);
  }

  downloadReportPdf(): void {
    if (!this.kurinKey || this.reportDownloading) return;

    this.reportDownloading = true;
    this.kurinService.downloadReportPdf(this.kurinKey).subscribe({
      next: (response) => {
        if (response.body) {
          this.saveBlob(response.body, this.resolveReportFileName(response));
        }
      },
      error: (error) => {
        console.error('Error downloading kurin report:', error);
        this.reportDownloading = false;
      },
      complete: () => {
        this.reportDownloading = false;
      }
    });
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

  private resolveReportFileName(response: HttpResponse<Blob>): string {
    const disposition = response.headers.get('content-disposition');
    const match = disposition?.match(/filename\*?=(?:UTF-8''|")?([^";]+)/i);
    if (match?.[1]) {
      return decodeURIComponent(match[1].replace(/"$/g, ''));
    }

    return `kurin-${this.kurinNumber ?? this.kurinKey}-report.pdf`;
  }

  private saveBlob(blob: Blob, fileName: string): void {
    const url = window.URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    document.body.appendChild(anchor);
    anchor.click();
    anchor.remove();
    window.URL.revokeObjectURL(url);
  }
}
