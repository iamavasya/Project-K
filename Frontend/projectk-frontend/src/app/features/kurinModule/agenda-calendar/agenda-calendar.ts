import { ChangeDetectionStrategy, Component, computed, inject, OnInit, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import {
  addMonths, eachDayOfInterval, endOfMonth, endOfWeek, format, isSameMonth, isToday,
  parseISO, startOfDay, startOfMonth, startOfWeek
} from 'date-fns';
import { AgendaService } from '../common/services/agenda-service/agenda-service';
import { PermissionService } from '../../authModule/services/permission.service';
import { AgendaItemDialogComponent } from '../common/components/agenda-item-dialog/agenda-item-dialog';
import { AgendaItemDto } from '../common/models/agenda';
import { AGENDA_STATUS_META, TagSeverity } from '../common/models/agenda-status.config';

interface CalendarDay {
  date: Date;
  key: string;
  dayNumber: string;
  inMonth: boolean;
  isToday: boolean;
  items: AgendaItemDto[];
}

const UA_MONTHS = [
  'Січень', 'Лютий', 'Березень', 'Квітень', 'Травень', 'Червень',
  'Липень', 'Серпень', 'Вересень', 'Жовтень', 'Листопад', 'Грудень'
];

@Component({
  selector: 'app-agenda-calendar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, TagModule, AgendaItemDialogComponent],
  template: `
    <div class="agenda-page">
      <section class="kurin-tile">
        <div class="agenda-header">
          <h1 class="agenda-title">Календар</h1>
          @if (canManage()) {
            <p-button label="Нова подія" icon="pi pi-plus" (click)="openCreate()" />
          }
        </div>

        <div class="agenda-toolbar">
          <p-button icon="pi pi-chevron-left" severity="secondary" [text]="true" [rounded]="true" (click)="previousMonth()" ariaLabel="Попередній місяць" />
          <span class="agenda-month">{{ monthLabel() }}</span>
          <p-button icon="pi pi-chevron-right" severity="secondary" [text]="true" [rounded]="true" (click)="nextMonth()" ariaLabel="Наступний місяць" />
          <p-button label="Сьогодні" severity="secondary" [text]="true" (click)="goToday()" />
        </div>

        <div class="agenda-grid" role="grid">
          @for (weekday of weekdays; track weekday) {
            <div class="agenda-grid__weekday" role="columnheader">{{ weekday }}</div>
          }
          @for (day of days(); track day.key) {
            <div class="agenda-cell" [class.agenda-cell--muted]="!day.inMonth" [class.agenda-cell--today]="day.isToday" role="gridcell">
              <div class="agenda-cell__num">{{ day.dayNumber }}</div>
              <div class="agenda-cell__items">
                @for (item of day.items; track item.agendaItemKey) {
                  <button type="button" class="agenda-chip" (click)="openItem(item)">
                    <p-tag [severity]="severityFor(item)" [value]="item.title" />
                  </button>
                }
              </div>
            </div>
          }
        </div>
      </section>
    </div>

    <app-agenda-item-dialog
      [visible]="dialogVisible()"
      (visibleChange)="dialogVisible.set($event)"
      [kurinKey]="kurinKey()"
      [item]="editing()"
      (saved)="loadData()" />
  `,
  styles: [`
    .agenda-page { margin-inline: auto; padding-block: 2rem; width: min(100% - 2rem, 72rem); }
    .agenda-header { align-items: center; display: flex; gap: 1rem; justify-content: space-between; padding-bottom: 1rem; }
    .agenda-title { color: var(--p-text-color); font-size: 1.5rem; font-weight: 800; letter-spacing: -0.02em; margin: 0; }
    .agenda-toolbar { align-items: center; display: flex; gap: 0.5rem; padding-bottom: 1rem; }
    .agenda-month { font-weight: 700; min-width: 9rem; text-align: center; }
    .agenda-grid { display: grid; gap: 1px; grid-template-columns: repeat(7, minmax(0, 1fr)); background: var(--p-content-border-color); border: 1px solid var(--p-content-border-color); border-radius: 12px; overflow: hidden; }
    .agenda-grid__weekday { background: var(--p-surface-ground); color: var(--p-text-muted-color); font-size: 0.72rem; font-weight: 700; letter-spacing: 0.05em; padding: 0.5rem; text-align: center; text-transform: uppercase; }
    .agenda-cell { background: var(--p-content-background); display: flex; flex-direction: column; gap: 0.3rem; min-height: 6.5rem; padding: 0.4rem; }
    .agenda-cell--muted { background: var(--p-surface-ground); }
    .agenda-cell--muted .agenda-cell__num { color: var(--p-text-muted-color); }
    .agenda-cell--today .agenda-cell__num { background: var(--p-primary-color); border-radius: 50%; color: var(--p-primary-contrast-color); }
    .agenda-cell__num { align-self: flex-start; font-size: 0.8rem; font-weight: 650; height: 1.5rem; line-height: 1.5rem; min-width: 1.5rem; text-align: center; }
    .agenda-cell__items { display: flex; flex-direction: column; gap: 0.25rem; }
    .agenda-chip { background: none; border: 0; cursor: pointer; padding: 0; text-align: left; width: 100%; }
    .agenda-chip :is(.p-tag) { max-width: 100%; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }
    @media (max-width: 640px) {
      .agenda-cell { min-height: 4.5rem; }
      .agenda-grid__weekday { font-size: 0.6rem; padding: 0.3rem 0.1rem; }
    }
  `]
})
export class AgendaCalendarComponent implements OnInit {
  private readonly agendaService = inject(AgendaService);
  private readonly permissionService = inject(PermissionService);
  private readonly route = inject(ActivatedRoute);

  protected readonly weekdays = ['Пн', 'Вт', 'Ср', 'Чт', 'Пт', 'Сб', 'Нд'];

  protected readonly kurinKey = signal('');
  protected readonly currentMonth = signal(startOfMonth(new Date()));
  protected readonly items = signal<AgendaItemDto[]>([]);
  protected readonly dialogVisible = signal(false);
  protected readonly editing = signal<AgendaItemDto | null>(null);

  protected readonly monthLabel = computed(() => {
    const month = this.currentMonth();
    return `${UA_MONTHS[month.getMonth()]} ${month.getFullYear()}`;
  });

  protected readonly days = computed<CalendarDay[]>(() => {
    const gridStart = startOfWeek(startOfMonth(this.currentMonth()), { weekStartsOn: 1 });
    const gridEnd = endOfWeek(endOfMonth(this.currentMonth()), { weekStartsOn: 1 });
    const byDay = this.indexItemsByDay(this.items());

    return eachDayOfInterval({ start: gridStart, end: gridEnd }).map(date => {
      const key = format(date, 'yyyy-MM-dd');
      return {
        date,
        key,
        dayNumber: format(date, 'd'),
        inMonth: isSameMonth(date, this.currentMonth()),
        isToday: isToday(date),
        items: byDay.get(key) ?? []
      };
    });
  });

  canManage(): boolean {
    return this.permissionService.canManageAgenda();
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      this.kurinKey.set(params.get('kurinKey') ?? '');
      this.loadData();
    });
  }

  loadData(): void {
    if (!this.kurinKey()) {
      return;
    }
    const from = startOfWeek(startOfMonth(this.currentMonth()), { weekStartsOn: 1 });
    const to = endOfWeek(endOfMonth(this.currentMonth()), { weekStartsOn: 1 });
    this.agendaService.getCalendar(this.kurinKey(), from.toISOString(), to.toISOString())
      .subscribe(items => this.items.set(items));
  }

  previousMonth(): void {
    this.currentMonth.set(addMonths(this.currentMonth(), -1));
    this.loadData();
  }

  nextMonth(): void {
    this.currentMonth.set(addMonths(this.currentMonth(), 1));
    this.loadData();
  }

  goToday(): void {
    this.currentMonth.set(startOfMonth(new Date()));
    this.loadData();
  }

  openCreate(): void {
    this.editing.set(null);
    this.dialogVisible.set(true);
  }

  openItem(item: AgendaItemDto): void {
    this.editing.set(item.canEdit ? item : null);
    if (item.canEdit) {
      this.dialogVisible.set(true);
    }
  }

  severityFor(item: AgendaItemDto): TagSeverity {
    return item.kind === 'Task' ? AGENDA_STATUS_META[item.status].severity : 'secondary';
  }

  private indexItemsByDay(items: AgendaItemDto[]): Map<string, AgendaItemDto[]> {
    const map = new Map<string, AgendaItemDto[]>();
    for (const item of items) {
      if (!item.startUtc) {
        continue;
      }
      const start = startOfDay(parseISO(item.startUtc));
      const end = startOfDay(parseISO(item.endUtc ?? item.startUtc));
      for (const day of eachDayOfInterval({ start, end })) {
        const key = format(day, 'yyyy-MM-dd');
        const bucket = map.get(key);
        if (bucket) {
          bucket.push(item);
        } else {
          map.set(key, [item]);
        }
      }
    }
    return map;
  }
}
