import { Component, inject, OnInit } from '@angular/core';
import { TableModule } from 'primeng/table';
import { SplitButtonModule } from 'primeng/splitbutton';

import { KurinDto } from '../common/models/kurinDto';
import { KurinService } from '../common/services/kurin-service/kurin.service';
import { MenuItem } from 'primeng/api';
import { ManageAction, ManagePanel, ManagePanelConfig } from '../common/components/manage-panel/manage-panel';
import { ButtonModule } from 'primeng/button';
import { Router } from '@angular/router';
import { MessageModule } from 'primeng/message';
import { AuthService } from '../../authModule/services/authService/auth.service';

@Component({
  selector: 'app-admin-panel',
  imports: [TableModule, SplitButtonModule, ManagePanel, ButtonModule, MessageModule],
  templateUrl: './admin-panel.component.html',
  styleUrls: ['./admin-panel.component.css']
})
export class AdminPanelComponent implements OnInit {

  private readonly router: Router = inject(Router);
  private readonly kurinService = inject(KurinService);
  private readonly authService = inject(AuthService);
  
  selectedItem: KurinDto | null = null;
  managePanelVisible = false;
  managePanelParameter: 'create' | 'update' | 'delete' | 'undef' = 'undef';
  data: KurinDto[] = [];

  tableHeaders = [
    "KurinKey",
    "Number"
  ];

  actions: MenuItem[] = [];

  managePanelConfig: ManagePanelConfig = {
    entityType: 'kurin',
    title: 'Kurin',
    fields: [
      {
        name: 'kurinKey',
        label: 'Kurin Key',
        type: 'text',
        required: true,
        hiddenOn: ['create', 'delete'],
        disabledOn: ['update']
      },
      {
        name: 'number',
        label: 'Number',
        type: 'number',
        required: true,
        hiddenOn: ['delete'],
      },
      {
        name: 'managerEmail',
        label: 'Manager Email',
        type: 'text',
        required: true,
        hiddenOn: ['delete'],
        disabledOn: ['update']
      }
    ],
    createFactory: () => ({ kurinKey: '', number: null, managerEmail: '' }),
  }

  prepareItemActions(item: KurinDto): void {
    this.actions = [
      {
        label: 'Update',
        command: () => { this.onActionClick(item, 'update') }
      },
      {
        label: 'Delete',
        command: () => { this.onActionClick(item, 'delete') }
      }
    ];
  }

  ngOnInit(): void {
    this.refreshData();
  }

  onActionClick(item: KurinDto | null, param: ManageAction | 'undef'): void {
    this.selectedItem = item;
    this.managePanelVisible = true;
    this.managePanelParameter = param;
  }

  onManageAction(e: { action: ManageAction; entity: KurinDto; entityType: string }): void {
    switch (e.action) {
      case 'create':
        this.authService.registerFirstManager(e.entity).subscribe(() => { this.refreshData(); });
        break;
      case 'update':
        this.kurinService.updateKurin(e.entity).subscribe(() => this.refreshData());
        break;
      case 'delete':
        this.kurinService.deleteKurin(e.entity.kurinKey).subscribe(() => this.refreshData());
        break;
    }
  }

  refreshData(): void {
    this.kurinService.getKurins().subscribe((data: KurinDto[]) => {
      this.data = data ?? [];
    });
  }

  onOpenClick(kurinKey: string): void {
    this.authService.setKurinKey(kurinKey);
    this.router.navigate(['/kurin']);
  }
}
