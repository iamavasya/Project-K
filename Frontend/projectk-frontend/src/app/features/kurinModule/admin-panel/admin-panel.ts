import { Component, inject, OnInit, ChangeDetectionStrategy } from '@angular/core';
import { TableModule } from '@openng/optimus-ui/table';
import { SplitButtonModule } from '@openng/optimus-ui/splitbutton';

import { KurinDto } from '../common/models/kurinDto';
import { KurinService } from '../common/services/kurin-service/kurin.service';
import { MenuItem, MessageService } from '@openng/optimus-ui/api';
import { ManageAction, ManagePanelComponent, ManagePanelConfig } from '../common/components/manage-panel/manage-panel';
import { ButtonModule } from '@openng/optimus-ui/button';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../authModule/services/authService/auth.service';
import { EmptyStateComponent } from '../../../shared/empty-state/empty-state';

@Component({
  selector: 'app-admin-panel',
  imports: [TableModule, SplitButtonModule, ManagePanelComponent, ButtonModule, EmptyStateComponent, RouterModule],
  templateUrl: './admin-panel.html',
  changeDetection: ChangeDetectionStrategy.Eager,
  styleUrl: './admin-panel.css'
})
export class AdminPanelComponent implements OnInit {

  private readonly router: Router = inject(Router);
  private readonly kurinService = inject(KurinService);
  private readonly authService = inject(AuthService);
  private readonly messageService = inject(MessageService);

  selectedItem: KurinDto | null = null;
  managePanelVisible = false;
  managePanelParameter: 'create' | 'update' | 'delete' | 'undef' = 'undef';
  data: KurinDto[] = [];

  actions: MenuItem[] = [];

  managePanelConfig: ManagePanelConfig = {
    entityType: 'kurin',
    title: 'Курінь',
    fields: [
      {
        name: 'kurinKey',
        label: 'Системний ключ',
        type: 'text',
        required: true,
        hiddenOn: ['create', 'delete'],
        disabledOn: ['update']
      },
      {
        name: 'number',
        label: 'Номер куреня',
        type: 'number',
        placeholder: 'Наприклад: 101',
        required: true,
        hiddenOn: ['delete'],
      },
      {
        name: 'managerEmail',
        label: 'Email звʼязкового',
        type: 'text',
        placeholder: 'manager@example.com',
        required: true,
        hiddenOn: ['delete'],
        disabledOn: ['update']
      }
    ],
    displayName: (entity: KurinDto) => `${entity.number} курінь`,
    createFactory: () => ({ kurinKey: '', number: null, managerEmail: '' }),
  }

  prepareItemActions(item: KurinDto): void {
    this.actions = [
      {
        label: 'Редагувати',
        icon: 'pi pi-pencil',
        command: () => { this.onActionClick(item, 'update') }
      },
      {
        label: 'Видалити',
        icon: 'pi pi-trash',
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
    this.authService.setKurinScope(kurinKey).subscribe({
      next: () => this.router.navigate(['/kurin']),
      error: () => this.messageService.add({
        severity: 'error',
        summary: 'Не вдалося відкрити курінь',
        detail: 'Спробуй ще раз.'
      })
    });
  }
}
