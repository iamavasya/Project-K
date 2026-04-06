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

@Component({
  selector: 'app-planning-list',
  standalone: true,
  imports: [CommonModule, TableModule, ButtonModule, TagModule, RouterModule, PlanningDetailComponent],
  template: `
    <div class="card p-6">
      <div class="flex justify-between items-center mb-6">
        <h1 class="text-2xl font-bold text-slate-800">Планування Таборів</h1>
        <p-button
          label="Створити нове"
          icon="pi pi-plus"
          (click)="createNew()" />
        </div>
    
        <p-table [value]="sessions()" [tableStyle]="{ 'min-width': '50rem' }">
          <ng-template pTemplate="header">
            <tr>
              <th>Назва</th>
              <th>Період пошуку</th>
              <th>Статус</th>
              <th>Дії</th>
            </tr>
          </ng-template>
          <ng-template pTemplate="body" let-session>
            <tr>
              <td class="font-semibold">{{ session.name }}</td>
              <td>
                {{ session.searchStart | date:'dd.MM' }} - {{ session.searchEnd | date:'dd.MM.yyyy' }}
              </td>
              <td>
                <p-tag
                  [severity]="session.isCalculated ? 'success' : 'info'"
                  [value]="session.isCalculated ? 'Розраховано' : 'Нове'" />
                </td>
                <td>
                  <div class="flex gap-2">
                    <p-button icon="pi pi-eye" severity="secondary" [rounded]="true" [text]="true" (click)="openDetails(session.planningSessionKey)" pTooltip="Переглянути графік"/>
                    <p-button icon="pi pi-trash" severity="danger" [rounded]="true" [text]="true" (click)="delete(session.planningSessionKey)" />
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
    `
})
export class PlanningListComponent implements OnInit {
  private readonly planningService = inject(PlanningService);
  private readonly memberService = inject(MemberService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  
  sessions = signal<PlanningSessionDto[]>([]);

  detailsVisible = false;
  selectedSessionId: string | null = null;

  kurinKey = ''; 

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