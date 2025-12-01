import { Component, EventEmitter, Input, Output, inject, signal, OnChanges, SimpleChanges, effect } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PlanningService } from '../../services/planning-service/planning-service';
import { PlanningSessionDto } from '../../models/planningSessionDto';

// PrimeNG Imports
import { DialogModule } from 'primeng/dialog';
import { ChartModule } from 'primeng/chart'; // <--- CHART
import { SkeletonModule } from 'primeng/skeleton';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { DividerModule } from 'primeng/divider';

import 'chartjs-adapter-date-fns'; // ВАЖЛИВО: Імпорт адаптера дат
import { uk } from 'date-fns/locale'; // Локалізація для дат

@Component({
  selector: 'app-planning-detail',
  standalone: true,
  imports: [
    CommonModule, 
    DialogModule, 
    ChartModule, 
    SkeletonModule, 
    ButtonModule, 
    TagModule,
    DividerModule
  ],
  template: `
<p-dialog 
      [(visible)]="visible" 
      [style]="{ width: '95vw', maxWidth: '1200px' }" 
      [header]="'Деталі: ' + (session()?.name || '...')" 
      [modal]="true" 
      [draggable]="false" 
      [resizable]="false"
      [dismissableMask]="true"
      appendTo="body"
      (onHide)="close()">

      <ng-template pTemplate="content">
        
        <div *ngIf="loading(); else realContent" class="flex flex-col gap-4">
          <p-skeleton height="100px" width="100%" />
          <p-skeleton height="300px" width="100%" />
        </div>

        <ng-template #realContent>
          <div *ngIf="session() as s" class="flex flex-col gap-6">

            <div class="bg-slate-50 p-4 rounded-xl border border-slate-200 flex flex-col md:flex-row justify-between items-start md:items-center gap-4">
              <div>
                <div class="text-xs text-slate-500 font-bold uppercase tracking-wider">Період пошуку</div>
                <div class="font-bold text-lg text-slate-800">
                  {{ s.searchStart | date:'dd.MM.yyyy' }} — {{ s.searchEnd | date:'dd.MM.yyyy' }}
                </div>
              </div>

              <div *ngIf="s.isCalculated" class="w-full md:w-auto flex items-center gap-3 bg-white px-4 py-2 rounded-lg border border-green-200 shadow-sm">
                <i class="pi pi-check-circle text-2xl text-green-500 shrink-0"></i>
                <div>
                  <div class="text-[10px] text-green-700 font-bold uppercase">Оптимальна дата</div>
                  <div class="text-xl font-bold leading-none mt-1">
                    {{ s.optimalStartDate | date:'dd.MM' }} — {{ s.optimalEndDate | date:'dd.MM' }}
                  </div>
                </div>
              </div>
            </div>

            <div class="border border-slate-200 rounded-xl p-2 md:p-4 overflow-hidden bg-white shadow-sm">
              <h3 class="font-bold text-slate-700 mb-4 ml-2">Графік зайнятості</h3>
              
              <div class="relative w-full" *ngIf="chartData">
                <p-chart 
                  type="bar" 
                  [data]="chartData" 
                  [options]="chartOptions" 
                  [height]="calculateHeight()" 
                  [responsive]="true">
                </p-chart>
              </div>

              <div *ngIf="!chartData" class="text-center p-4 text-slate-400">
                Немає даних для відображення
              </div>
            </div>

          </div>
        </ng-template>
      </ng-template> <ng-template pTemplate="footer">
        <p-button label="Закрити" (click)="close()" [text]="true" severity="secondary" />
      </ng-template>
    </p-dialog>
  `
})
export class PlanningDetailComponent implements OnChanges {
  @Input() visible = false;
  @Input() sessionId: string | null = null;
  @Output() visibleChange = new EventEmitter<boolean>();

  private readonly service = inject(PlanningService);
  
  session = signal<PlanningSessionDto | null>(null);
  loading = signal(false);

  // Дані для Chart.js
  chartData: any;
  chartOptions: any;

  ngOnChanges(changes: SimpleChanges) {
    if (changes['visible'] && this.visible && this.sessionId) {
      this.loadData(this.sessionId);
    }
  }

  loadData(id: string) {
    this.loading.set(true);
    this.service.getSessionByKey(id).subscribe({
      next: (data) => {
        this.session.set(data);
        this.initChart(data); // Ініціалізуємо графік
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  close() {
    this.visible = false;
    this.visibleChange.emit(false);
  }

  calculateHeight() {
    // 50px на людину + 100px буфер
    const count = this.session()?.participants.length || 5;
    return `${Math.max(300, count * 50 + 100)}px`;
  }

  initChart(s: PlanningSessionDto) {
    const documentStyle = getComputedStyle(document.documentElement);
    const textColor = documentStyle.getPropertyValue('--text-color');
    const textColorSecondary = documentStyle.getPropertyValue('--text-color-secondary');
    const surfaceBorder = documentStyle.getPropertyValue('--surface-border');

    const labels = ['Табір', ...s.participants.map(p => p.fullName)];

    // 2. Формуємо Dataset для Оптимальної дати (Зелений)
    const optimalData = [];
    if (s.isCalculated && s.optimalStartDate && s.optimalEndDate) {
      optimalData.push({
        x: [s.optimalStartDate, s.optimalEndDate], // Floating bar [Start, End]
        y: labels[0] // Прив'язка до першого рядка
      });
    }

    // 3. Формуємо Dataset для Зайнятості (Червоний)
    const busyData: any[] = [];
    s.participants.forEach(p => {
      p.busyRanges.forEach(range => {
        busyData.push({
          x: [range.start, range.end],
          y: p.fullName // Прив'язка до імені
        });
      });
    });

    // 4. Заповнюємо об'єкт даних
    this.chartData = {
      labels: labels,
      datasets: [
        {
          label: 'Оптимальний час',
          backgroundColor: '#22c55e', // green-500
          borderColor: '#16a34a',
          borderWidth: 1,
          barPercentage: 0.6,
          data: optimalData
        },
        {
          label: 'Зайнятий',
          backgroundColor: '#f87171', // red-400
          borderColor: '#ef4444',
          borderWidth: 1,
          barPercentage: 0.5,
          data: busyData
        }
      ]
    };

    // 5. Налаштування (Options)
    this.chartOptions = {
      indexAxis: 'y', // РОБИТЬ ГРАФІК ГОРИЗОНТАЛЬНИМ (Gantt style)
      maintainAspectRatio: false,
      aspectRatio: 0.8,
      plugins: {
        legend: {
          labels: { color: textColor }
        },
        tooltip: {
          callbacks: {
            // Форматуємо дату в тултіпі
            label: (context: any) => {
              const start = new Date(context.raw.x[0]).toLocaleDateString('uk-UA', {day: '2-digit', month: '2-digit'});
              const end = new Date(context.raw.x[1]).toLocaleDateString('uk-UA', {day: '2-digit', month: '2-digit'});
              return `${context.dataset.label}: ${start} - ${end}`;
            }
          }
        }
      },
      scales: {
        x: {
          type: 'time', // Важливо: вісь часу
          time: {
            unit: 'day', // Показувати дні
            tooltipFormat: 'dd.MM.yyyy',
            displayFormats: {
              day: 'dd MMM'
            }
          },
          min: s.searchStart, // Обрізаємо графік по межах пошуку
          max: s.searchEnd,
          ticks: {
            color: textColorSecondary
          },
          grid: {
            color: surfaceBorder,
            drawBorder: false
          }
        },
        y: {
          ticks: {
            color: textColor,
            font: {
              weight: 'bold'
            }
          },
          grid: {
            color: surfaceBorder,
            drawBorder: false
          }
        }
      }
    };
  }
}