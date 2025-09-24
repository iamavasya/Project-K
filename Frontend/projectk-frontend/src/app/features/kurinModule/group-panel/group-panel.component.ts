import { Component, inject, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { GroupService } from '../common/services/group-service/group.service';
import { GroupDto } from '../common/models/groupDto';
import { SplitButton } from 'primeng/splitbutton';
import { ManageAction, ManagePanel, ManagePanelConfig } from '../common/components/manage-panel/manage-panel';
import { MenuItem } from 'primeng/api';
import { MessageModule } from 'primeng/message';
import { KurinService } from '../common/services/kurin-service/kurin.service';
import { KurinNumberComponent } from '../common/components/kurin-number/kurin-number';
import { AuthService } from '../../authModule/services/auth.service';

@Component({
  selector: 'app-group-panel',
  imports: [TableModule, ButtonModule, SplitButton, ManagePanel, MessageModule, KurinNumberComponent],
  templateUrl: './group-panel.component.html',
  styleUrls: ['./group-panel.component.scss']
})
export class GroupPanelComponent implements OnInit {

  private readonly route: ActivatedRoute = inject(ActivatedRoute);
  private readonly router: Router = inject(Router);
  private readonly groupService = inject(GroupService);
  private readonly kurinService = inject(KurinService);
  private readonly authService = inject(AuthService);
  groups: GroupDto[] = [];
  actions: MenuItem[] = [];

  tableHeaders: string[] = ['GroupKey', 'GroupName', 'KurinNumber'];

  kurinKey = '';
  kurinNumber: number | null = null;

  groupPanelConfig: ManagePanelConfig = {
    entityType: 'group',
    title: 'Group',
    fields: [
      {
        name: 'groupKey',
        label: 'Group Key',
        type: 'text',
        required: true,
        hiddenOn: ['create', 'delete'],
        disabledOn: ['update']
      },
      {
        name: 'kurinKey',
        label: 'Kurin Key',
        type: 'text',
        required: true,
        hiddenOn: ['create', 'delete', 'update'],
        disabledOn: ['update']
      },
      {
        name: 'name',
        label: 'Name',
        type: 'text',
        required: true,
        hiddenOn: ['delete']
      }
    ],
    createFactory: () => ({ groupKey: '', name: '', kurinKey: this.kurinKey })
  };

  groupPanelVisible = false;
  groupPanelParameter: ManageAction | 'undef' = 'undef';
  selectedGroup: GroupDto | null = null;

  ngOnInit() {
    this.authService.getAuthState().subscribe(state => {
      if (state?.kurinKey) {
        this.kurinKey = state.kurinKey;
      }
    });
    this.refreshData();
  }

  refreshData() {
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
      },
      error: (error) => {
        console.error('Error fetching kurin:', error);
      }
    });
  }

  prepareItemActions(item: GroupDto): void {
    this.actions = [
      {
        label: 'Update',
        command: () => { this.onGroupActionClick(item, 'update') }
      },
      {
        label: 'Delete',
        command: () => { this.onGroupActionClick(item, 'delete') }
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
}