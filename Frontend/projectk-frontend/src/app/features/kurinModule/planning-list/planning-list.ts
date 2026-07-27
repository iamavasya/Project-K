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
    <div class="planning-page">
      <section class="kurin-tile">
        <div class="planning-header">
          <h1 class="planning-title">Планування таборів</h1>
          @if (canManagePlanning) {
            <p-button
              label="Створити нове"
              icon="pi pi-plus"
              (click)="createNew()" />
          }
        </div>

        <p-table [value]="sessions()" styleClass="project-table planning-table">
          <ng-template pTemplate="header">
            <tr>
              <th scope="col">Назва</th>
              <th scope="col">Період пошуку</th>
              <th scope="col">Статус</th>
              <th scope="col" class="planning-actions-col">Дії</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-session>
            <tr class="planning-row">
              <td class="planning-name" data-mobile-label="Назва">{{ session.name }}</td>
              <td data-mobile-label="Період">
                {{ session.searchStart | date:'dd.MM' }} - {{ session.searchEnd | date:'dd.MM.yyyy' }}
              </td>
              <td data-mobile-label="Статус">
                <p-tag
                  [severity]="session.isCalculated ? 'success' : 'warn'"
                  [value]="session.isCalculated ? 'Розраховано' : 'Нове'" />
              </td>
              <td class="planning-actions-col" data-mobile-label="Дії">
                <div class="planning-actions">
                  <p-button icon="pi pi-eye" severity="secondary" [rounded]="true" [text]="true" (click)="openDetails(session.planningSessionKey)" pTooltip="Переглянути графік"/>
                  @if (canManagePlanning) {
                    <p-button icon="pi pi-trash" severity="danger" [rounded]="true" [text]="true" (click)="delete(session.planningSessionKey)" />
                  }
                </div>
              </td>
            </tr>
          </ng-template>
          <ng-template pTemplate="emptymessage">
            <tr>
              <td colspan="4" class="planning-empty-cell">
                <div class="lil-empty">
                  <svg class="lil-empty__art" viewBox="0 0 110 66" fill="none"
                       stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true">
                    <rect x="18" y="14" width="74" height="42" rx="4" />
                    <path d="M18 27 H92" />
                    <path class="lil-empty__accent" d="M34 41 H54" />
                    <path d="M34 8 V16" />
                    <path d="M76 8 V16" />
                  </svg>
                  <div class="lil-empty__title">Календар ще спить</div>
                  <div class="lil-empty__body">Перша сходина в плані — і він прокинеться.</div>
                  @if (canManagePlanning) {
                    <p-button label="Створити планування" icon="pi pi-plus" severity="secondary" (click)="createNew()" />
                  }
                </div>
              </td>
            </tr>
          </ng-template>
        </p-table>
      </section>

      @if (detailsVisible) {
        <app-planning-detail
          [(visible)]="detailsVisible"
          [sessionId]="selectedSessionId">
        </app-planning-detail>
      }
    </div>
  `,
  styles: [`
    .planning-page {
      margin-inline: auto;
      padding-block: 2rem;
      width: min(100% - 2rem, 72rem);
    }

    .planning-header {
      align-items: center;
      display: flex;
      gap: 1rem;
      justify-content: space-between;
      padding-bottom: 1rem;
    }

    .planning-title {
      color: var(--p-text-color);
      font-size: 1.5rem;
      font-weight: 800;
      letter-spacing: -0.02em;
      margin: 0;
    }

    .planning-name {
      font-weight: 650;
    }

    .planning-actions-col {
      text-align: right;
      width: 6rem;
    }

    .planning-actions {
      display: flex;
      gap: 0.125rem;
      justify-content: flex-end;
    }

    .planning-empty-cell {
      border: 0 !important;
      padding: 1.5rem 0 0 !important;
    }

    :host ::ng-deep .planning-table .p-datatable-table {
      width: 100% !important;
      min-width: 0 !important;
    }

    @media (max-width: 768px) {
      .planning-header {
        align-items: stretch;
        flex-direction: column;
      }
    }

    @media (max-width: 640px) {
      :host ::ng-deep .planning-table .p-datatable-wrapper {
        overflow-x: visible;
      }

      :host ::ng-deep .planning-table .p-datatable-thead {
        display: none;
      }

      :host ::ng-deep .planning-table .p-datatable-tbody > tr.planning-row {
        border-bottom: 1px solid var(--p-content-border-color);
        display: grid;
        gap: 0.45rem;
        padding: 0.8rem 0;
      }

      :host ::ng-deep .planning-table .p-datatable-tbody > tr.planning-row > td {
        align-items: center;
        border: 0 !important;
        display: grid;
        gap: 0.6rem;
        grid-template-columns: 5.25rem minmax(0, 1fr);
        min-width: 0;
        overflow-wrap: anywhere;
        padding: 0.1rem 0 !important;
      }

      :host ::ng-deep .planning-table .p-datatable-tbody > tr.planning-row > td::before {
        color: var(--p-text-muted-color);
        content: attr(data-mobile-label);
        font-size: 0.75rem;
        font-weight: 650;
      }

      .planning-actions-col {
        text-align: left;
        width: auto;
      }

      .planning-actions {
        justify-content: flex-start;
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
