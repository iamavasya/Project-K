import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { PlanningService } from '../common/services/planning-service/planning-service';
import { MemberService } from '../common/services/member-service/member.service';
import { PlanningSessionDto } from '../common/models/planningSessionDto';
import { PlanningDetailComponent } from '../common/components/planning-detail/planning-detail';
import { PermissionService } from '../../authModule/services/permission.service';

@Component({
  selector: 'app-planning-list',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, TagModule, RouterModule, PlanningDetailComponent],
  template: `
    <div class="card p-6">
      <div class="flex justify-between items-center mb-6">
        <h1 class="text-2xl font-bold text-slate-800">Планування Таборів</h1>
        <p-button
          *ngIf="canManagePlanning"
          label="Створити нове"
          icon="pi pi-plus"
          (click)="createNew()" />
        </div>
    
        <p-table [value]="sessions()" styleClass="planning-table">
          <ng-template pTemplate="header">
            <tr>
              <th>Назва</th>
              <th>Період пошуку</th>
              <th>Статус</th>
              <th>Дії</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-session>
            <tr class="planning-row">
              <td class="font-semibold" data-mobile-label="Назва">{{ session.name }}</td>
              <td data-mobile-label="Період">
                {{ session.searchStart | date:'dd.MM' }} - {{ session.searchEnd | date:'dd.MM.yyyy' }}
              </td>
              <td data-mobile-label="Статус">
                <p-tag
                  [severity]="session.isCalculated ? 'success' : 'info'"
                  [value]="session.isCalculated ? 'Розраховано' : 'Нове'" />
                </td>
                <td data-mobile-label="Дії">
                  <div class="flex gap-2">
                    <p-button icon="pi pi-eye" severity="secondary" [rounded]="true" [text]="true" (click)="openDetails(session.planningSessionKey)" pTooltip="Переглянути графік"/>
                    <p-button *ngIf="canManagePlanning" icon="pi pi-trash" severity="danger" [rounded]="true" [text]="true" (click)="delete(session.planningSessionKey)" />
                  </div>
                </td>
              </tr>
            </ng-template>
            <ng-template pTemplate="emptymessage">
              <tr>
                <td colspan="4" class="text-center p-4">Сесій ще немає. Створіть першу!</td>
              </tr>
            </ng-template>
          </p-table>
    
          @if (detailsVisible) {
            <app-planning-detail
              [(visible)]="detailsVisible"
              [sessionId]="selectedSessionId">
            </app-planning-detail>
          }
    
        </div>
    `,
  styles: [`
    :host ::ng-deep .planning-table .p-datatable-table {
      width: 100% !important;
      min-width: 0 !important;
    }

    @media (max-width: 640px) {
      :host ::ng-deep .planning-table .p-datatable-wrapper {
        overflow-x: visible;
      }

      :host ::ng-deep .planning-table .p-datatable-thead {
        display: none;
      }

      :host ::ng-deep .planning-table .p-datatable-tbody > tr.planning-row {
        display: grid;
        gap: 0.45rem;
        padding: 0.8rem 0;
        border-bottom: 1px solid var(--p-content-border-color);
      }

      :host ::ng-deep .planning-table .p-datatable-tbody > tr.planning-row > td {
        display: grid;
        grid-template-columns: 5.25rem minmax(0, 1fr);
        gap: 0.6rem;
        align-items: center;
        min-width: 0;
        padding: 0.1rem 0 !important;
        border: 0 !important;
        overflow-wrap: anywhere;
      }

      :host ::ng-deep .planning-table .p-datatable-tbody > tr.planning-row > td::before {
        content: attr(data-mobile-label);
        color: var(--p-text-muted-color);
        font-size: 0.75rem;
        font-weight: 650;
      }
    }
  `]
})
export class PlanningListComponent implements OnInit {
  private readonly planningService = inject(PlanningService);
  private readonly memberService = inject(MemberService);
  private readonly permissionService = inject(PermissionService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  
  sessions = signal<PlanningSessionDto[]>([]);

  detailsVisible = false;
  selectedSessionId: string | null = null;

  kurinKey = ''; 

  get canManagePlanning(): boolean {
    return this.permissionService.canManagePlanning();
  }

  ngOnInit() {
    this.route.paramMap.subscribe(params => {
      this.kurinKey = params.get('kurinKey')!;
    });
    this.loadData();
  }

  loadData() {
    this.planningService.getSessions(this.kurinKey)
      .subscribe(data => {
        this.sessions.set(data);
      });
  }

  createNew() {
    this.router.navigate(['/planning/create', this.kurinKey]);
  }

  delete(id: string) {
    if(confirm('Видалити це планування?')) {
       this.planningService.deleteSession(id).subscribe(() => {
        this.loadData();
      });
    }
  }

  openDetails(id: string) {
    this.selectedSessionId = id;
    this.detailsVisible = true;
  }
}
