import { Component, inject, OnInit } from '@angular/core';
import { TableModule } from 'primeng/table';
import { SplitButtonModule } from 'primeng/splitbutton';
import { CommonModule } from '@angular/common';
import { KurinDto } from '../common/models/kurinDto';
import { KurinService } from '../common/services/kurin-service/kurin.service';
import { MenuItem } from 'primeng/api';
import { ManagePanel } from '../common/components/manage-panel/manage-panel';
import { ButtonModule } from 'primeng/button';
import { Router } from '@angular/router';

@Component({
  selector: 'app-kurin-panel',
  imports: [TableModule, SplitButtonModule, CommonModule, ManagePanel, ButtonModule],
  templateUrl: './kurin-panel.component.html',
  styleUrls: ['./kurin-panel.component.scss']
})
export class KurinPanelComponent implements OnInit {

  constructor(private router: Router) {}
  
  kurinService = inject(KurinService);
  data: KurinDto[] = [];
  selectedItem: KurinDto | null = null;
  managePanelVisible = false;
  managePanelParameter: 'create' | 'update' | 'delete' | 'undef' = 'undef';

  tableHeaders = [
    "KurinKey",
    "Number"
  ];

  actions: MenuItem[] = [];

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

  onActionClick(item: KurinDto | null, param: 'create' | 'update' | 'delete' | 'undef'): void {
    this.selectedItem = item;
    this.managePanelVisible = true;
    this.managePanelParameter = param;
  }

  actionHandler(action: { action: 'create' | 'update' | 'delete', kurin: KurinDto }): void {
    switch (action.action) {
      case 'create':
        this.kurinService.createKurin(action.kurin).subscribe(() => {
          this.refreshData();
        });
        break;
      case 'update':
        this.kurinService.updateKurin(action.kurin).subscribe(() => {
          this.refreshData();
        });
        break;
      case 'delete':
        this.kurinService.deleteKurin(action.kurin.kurinKey).subscribe(() => {
          this.refreshData();
        });
        break;
    }
  }

  refreshData(): void {
    this.kurinService.getKurins().subscribe((data: KurinDto[]) => {
      this.data = data ?? [];
      console.log('Kurins fetched:', this.data);
    });
  }

  onOpenClick(kurinKey: string): void {
    this.router.navigate(['/kurin', kurinKey]);
  }
}
