import { Component, ElementRef, OnDestroy, OnInit, afterRenderEffect, computed, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import {
  ActionSheetButton,
  ActionSheetController,
  IonButton,
  IonButtons,
  IonChip,
  IonContent,
  IonHeader,
  IonIcon,
  IonItem,
  IonItemGroup,
  IonItemOption,
  IonItemOptions,
  IonItemSliding,
  IonLabel,
  IonList,
  IonRefresher,
  IonRefresherContent,
  IonSearchbar,
  IonSegment,
  IonSegmentButton,
  IonSkeletonText,
  IonSpinner,
  IonTitle,
  IonToolbar,
  ViewWillEnter,
  IonMenuButton,
} from '@ionic/angular';
import { HeaderActions } from '../../nav/header-actions';
import { addIcons } from 'ionicons';
import { add, checkmarkCircle, chevronDown, ellipseOutline, timeOutline } from 'ionicons/icons';
import { AuthService } from '../../auth/auth.service';
import { apiErrorText } from '../../core/api';
import { FAILED_TEXT, Loaded, valueOf } from '../../core/loaded';
import { Toasts } from '../../core/toast';
import { GlassEffects } from '../../ui/glass';
import { confirmAgendaDelete } from '../agenda/agenda-item-view';
import {
  AgendaBoardFilter,
  AgendaBoardResponse,
  AgendaBoardSort,
  AgendaBoardTarget,
  AgendaItemDto,
  AgendaItemStatus,
  AgendaTargetType,
} from '../agenda/agenda.models';
import { SORT_OPTIONS, STATUSES, canManageAgenda, dueLabel, isLate, statusLabel, targetNote } from '../agenda/agenda.labels';
import { AgendaService } from '../agenda/agenda.service';

const PAGE = 20;

interface Column {
  status: AgendaItemStatus;
  label: string;
  total: number;
  items: AgendaItemDto[];
}

interface Board {
  columns: Column[];
  targets: AgendaBoardTarget[];
}

/**
 * The kurin's task board on a phone (the web's agenda-board): the three columns as a segment with
 * their counts, narrowed on the server by search, «Моє», target and order, each column paged with
 * «Завантажити ще». A task moves by its status button or a swipe, where the API's `canChangeStatus`
 * allows; editing, archiving and deleting follow `canEdit`.
 */
@Component({
  selector: 'app-tasks',
  imports: [
    IonMenuButton,
    HeaderActions,
    RouterLink,
    IonHeader,
    IonToolbar,
    IonTitle,
    IonButtons,
    IonButton,
    IonIcon,
    IonContent,
    IonRefresher,
    IonRefresherContent,
    IonSearchbar,
    IonChip,
    IonSegment,
    IonSegmentButton,
    IonLabel,
    IonList,
    IonItemGroup,
    IonItem,
    IonItemSliding,
    IonItemOptions,
    IonItemOption,
    IonSkeletonText,
    IonSpinner,
  ],
  styles: `
    .search {
      padding: 0 8px;
    }
    :host-context(.md) .search {
      padding: 8px 12px 0;
    }
    .filters {
      display: flex;
      gap: 4px;
      overflow-x: auto;
      padding: 0 12px 4px;
      scrollbar-width: none;
    }
    .filters ion-chip {
      flex: none;
      min-height: 36px;
    }
    .columns {
      padding: 8px 16px 0;
    }
    .mark {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 44px;
      height: 44px;
      margin: 0 4px 0 -10px;
      border: 0;
      background: transparent;
      font-size: 24px;
      color: var(--lk-faint);
    }
    .mark[data-status='Done'] {
      color: var(--lk-primary);
    }
    .mark[data-status='InProgress'] {
      color: var(--lk-accent-700);
    }
    .late {
      color: var(--lk-danger) !important;
    }
    .empty {
      margin: 16px 20px 0;
      color: var(--lk-muted);
    }
    :host-context(.ios) .empty {
      margin-inline: 32px;
    }
    .more {
      display: flex;
      justify-content: center;
      padding: 0 16px 24px;
    }
  `,
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        <ion-buttons slot="start"><ion-menu-button /></ion-buttons>
        <ion-title>Задачі</ion-title>
        <ion-buttons slot="end">
          @if (canManage()) {
            <ion-button routerLink="/tabs/tasks/new" aria-label="Нова задача" data-testid="new-task">
              <ion-icon slot="icon-only" name="add" />
            </ion-button>
          }
          <app-header-actions />
        </ion-buttons>
      </ion-toolbar>
    </ion-header>

    <ion-content [fullscreen]="true">
      <ion-refresher slot="fixed" (ionRefresh)="refresh($event)">
        <ion-refresher-content />
      </ion-refresher>
      <ion-header collapse="condense">
        <ion-toolbar><ion-title size="large">Задачі</ion-title></ion-toolbar>
      </ion-header>

      @if (!kurinKey) {
        <p class="empty">Ти ще не в курені, тож задач немає.</p>
      } @else {
        <div class="search">
          <ion-searchbar
            placeholder="Пошук за назвою, описом, гуртком"
            [debounce]="300"
            [value]="search()"
            (ionInput)="setSearch($event)"
            data-testid="search"
          />
        </div>
        <div class="filters">
          <ion-chip [outline]="!onlyMine()" [color]="onlyMine() ? 'primary' : undefined" (click)="toggleMine()" data-testid="mine">
            <ion-label>Моє</ion-label>
          </ion-chip>
          <ion-chip [outline]="true" (click)="chooseSort()" data-testid="sort">
            <ion-label>{{ sortLabel() }}</ion-label>
            <ion-icon name="chevron-down" />
          </ion-chip>
          @if (targets().length) {
            <ion-chip [outline]="!target()" [color]="target() ? 'primary' : undefined" (click)="chooseTarget()" data-testid="target-filter">
              <ion-label>{{ targetLabel() }}</ion-label>
              <ion-icon name="chevron-down" />
            </ion-chip>
          }
          @if (filtered()) {
            <ion-chip [outline]="true" (click)="resetFilters()"><ion-label>Скинути</ion-label></ion-chip>
          }
        </div>

        @switch (board().state) {
          @case ('loading') {
            <ion-list [inset]="true">
              <ion-item-group>
                <ion-item><ion-skeleton-text [animated]="true" style="height: 44px" /></ion-item>
                <ion-item><ion-skeleton-text [animated]="true" style="height: 44px" /></ion-item>
                <ion-item><ion-skeleton-text [animated]="true" style="height: 44px" /></ion-item>
              </ion-item-group>
            </ion-list>
          }
          @case ('failed') { <p class="empty">{{ failedText }}</p> }
          @default {
            @if (isEmpty()) {
              <p class="empty">{{ filtered() ? 'Нічого не знайдено.' : 'Дошка порожня.' }}</p>
            } @else {
              <div class="columns">
                <ion-segment [value]="active()" (ionChange)="setActive($event)" data-testid="columns">
                  @for (column of columns(); track column.status) {
                    <ion-segment-button [value]="column.status">
                      <ion-label>{{ column.label }} {{ column.total }}</ion-label>
                    </ion-segment-button>
                  }
                </ion-segment>
              </div>
              @if (column(); as current) {
                <ion-list [inset]="true" data-testid="column">
                  <ion-item-group>
                    @for (task of current.items; track task.agendaItemKey) {
                      <ion-item-sliding [disabled]="!task.canChangeStatus && !task.canEdit" data-testid="task-row">
                        @if (task.canChangeStatus) {
                          <ion-item-options side="start">
                            @for (to of movesFor(task); track to.value) {
                              <ion-item-option
                                [color]="to.value === 'Done' ? 'success' : to.value === 'InProgress' ? 'warning' : 'medium'"
                                [expandable]="$first"
                                (click)="move(task, to.value)"
                              >
                                {{ to.label }}
                              </ion-item-option>
                            }
                          </ion-item-options>
                        }
                        <ion-item [button]="true" [detail]="true" [routerLink]="['/tabs/tasks/task', task.agendaItemKey]">
                          <button
                            slot="start"
                            type="button"
                            class="mark"
                            [attr.data-status]="task.viewerStatus"
                            [attr.aria-label]="'Стан: ' + label(task.viewerStatus)"
                            [disabled]="!task.canChangeStatus || moving() !== null"
                            (click)="$event.stopPropagation(); $event.preventDefault(); chooseStatus(task)"
                            data-testid="status"
                          >
                            @if (moving() === task.agendaItemKey) {
                              <ion-spinner name="crescent" />
                            } @else {
                              <ion-icon [name]="icon(task.viewerStatus)" aria-hidden="true" />
                            }
                          </button>
                          <ion-label class="ion-text-wrap">
                            <h3>{{ task.title }}</h3>
                            @if (due(task); as when) { <p [class.late]="late(task)">{{ when }}</p> }
                            @if (targetsLine(task); as line) { <p>{{ line }}</p> }
                            @if (!task.addressedToViewer) { <p>Призначено іншим</p> }
                          </ion-label>
                        </ion-item>
                        @if (task.canEdit) {
                          <ion-item-options side="end">
                            <ion-item-option color="medium" (click)="more(task)">Ще</ion-item-option>
                            <ion-item-option color="danger" (click)="remove(task)">Видалити</ion-item-option>
                          </ion-item-options>
                        }
                      </ion-item-sliding>
                    } @empty {
                      <ion-item><ion-label>Тут порожньо.</ion-label></ion-item>
                    }
                  </ion-item-group>
                </ion-list>
                @if (current.items.length < current.total) {
                  <div class="more">
                    <ion-button fill="clear" size="default" [disabled]="loadingMore()" (click)="loadMore(current)" data-testid="load-more">
                      @if (loadingMore()) { <ion-spinner name="crescent" /> } @else {
                        Завантажити ще ({{ current.total - current.items.length }})
                      }
                    </ion-button>
                  </div>
                }
              }
            }
          }
        }
      }
    </ion-content>
  `,
})
export class TasksPage implements OnInit, ViewWillEnter, OnDestroy {
  private readonly agenda = inject(AgendaService);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toasts = inject(Toasts);
  private readonly sheets = inject(ActionSheetController);
  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef).nativeElement;
  private readonly glass = new GlassEffects(this.host);

  protected readonly kurinKey = this.auth.user()?.kurinKey ?? null;
  protected readonly canManage = computed(() => canManageAgenda(this.auth.user()));
  protected readonly failedText = FAILED_TEXT;
  protected readonly label = statusLabel;
  protected readonly due = dueLabel;
  protected readonly late = isLate;

  protected readonly search = signal('');
  protected readonly onlyMine = signal(false);
  protected readonly sort = signal<AgendaBoardSort>('Due');
  /** «Group:key» etc., as the web's target select keeps it. */
  protected readonly target = signal<string | null>(null);
  protected readonly active = signal<AgendaItemStatus>('Todo');
  protected readonly board = signal<Loaded<Board>>({ state: 'loading' });
  protected readonly loadingMore = signal(false);
  protected readonly moving = signal<string | null>(null);

  protected readonly columns = computed(() => valueOf(this.board())?.columns ?? []);
  protected readonly targets = computed(() => valueOf(this.board())?.targets ?? []);
  protected readonly column = computed(() => this.columns().find((c) => c.status === this.active()) ?? null);
  protected readonly filtered = computed(() => !!this.search().trim() || this.onlyMine() || !!this.target());
  protected readonly isEmpty = computed(() => this.columns().every((c) => c.total === 0));
  protected readonly sortLabel = computed(() => SORT_OPTIONS.find((o) => o.value === this.sort())?.label ?? '');
  protected readonly targetLabel = computed(() => {
    const value = this.target();
    const target = this.targets().find((t) => `${t.targetType}:${t.targetKey}` === value);
    return target ? boardTargetLabel(target) : 'Усі цілі';
  });

  private entered = false;

  constructor() {
    addIcons({ add, chevronDown, checkmarkCircle, ellipseOutline, timeOutline });
    afterRenderEffect(() => {
      this.board();
      this.glass.sync();
    });
  }

  ngOnInit(): void {
    void this.load();
  }

  /** Back from a task or a form: show what changed there. */
  ionViewWillEnter(): void {
    if (this.entered) void this.load();
    this.entered = true;
  }

  ngOnDestroy(): void {
    this.glass.destroy();
  }

  protected async refresh(event: Event): Promise<void> {
    await this.load();
    await (event.target as HTMLIonRefresherElement).complete();
  }

  private filter(): AgendaBoardFilter {
    const [targetType, targetKey] = this.target()?.split(':') ?? [null, null];
    return {
      search: this.search().trim() || null,
      targetType: (targetType as AgendaTargetType | null) ?? null,
      targetKey: targetKey ?? null,
      onlyMine: this.onlyMine() || undefined,
      sort: this.sort(),
    };
  }

  private async load(): Promise<void> {
    if (!this.kurinKey) return;
    try {
      const response = await this.agenda.board(this.kurinKey, { ...this.filter(), take: PAGE });
      this.board.set({ state: 'ready', value: toBoard(response) });
    } catch {
      if (this.board().state !== 'ready') this.board.set({ state: 'failed' });
    }
  }

  protected async loadMore(column: Column): Promise<void> {
    if (this.loadingMore() || !this.kurinKey) return;
    this.loadingMore.set(true);
    try {
      const page = await this.agenda.board(this.kurinKey, {
        ...this.filter(),
        status: column.status,
        skip: column.items.length,
        take: PAGE,
      });
      const more = page.columns[0];
      const board = valueOf(this.board());
      if (more && board) {
        this.board.set({
          state: 'ready',
          value: { ...board, columns: board.columns.map((c) => (c.status === column.status ? append(c, more.items, more.total) : c)) },
        });
      }
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося завантажити.'), 'danger');
    } finally {
      this.loadingMore.set(false);
    }
  }

  protected setSearch(event: Event): void {
    this.search.set(String((event as CustomEvent<{ value?: string | null }>).detail.value ?? ''));
    void this.load();
  }

  protected toggleMine(): void {
    this.onlyMine.update((on) => !on);
    void this.load();
  }

  protected resetFilters(): void {
    this.search.set('');
    this.onlyMine.set(false);
    this.target.set(null);
    void this.load();
  }

  protected setActive(event: Event): void {
    const value = (event as CustomEvent<{ value?: string }>).detail.value as AgendaItemStatus | undefined;
    if (value) this.active.set(value);
  }

  protected async chooseSort(): Promise<void> {
    const choice = await this.pick(
      'Сортування',
      SORT_OPTIONS.map((o) => ({ text: o.label, data: o.value, cssClass: o.value === this.sort() ? 'lk-chosen' : undefined })),
    );
    if (choice) {
      this.sort.set(choice as AgendaBoardSort);
      void this.load();
    }
  }

  protected async chooseTarget(): Promise<void> {
    const choice = await this.pick('Для кого', [
      { text: 'Усі цілі', data: '' },
      ...this.targets().map((t) => ({ text: boardTargetLabel(t), data: `${t.targetType}:${t.targetKey}` })),
    ]);
    if (choice !== undefined) {
      this.target.set(choice || null);
      void this.load();
    }
  }

  protected movesFor(task: AgendaItemDto): { value: AgendaItemStatus; label: string }[] {
    // The next step first: that is the one a full swipe takes.
    const order: Record<AgendaItemStatus, AgendaItemStatus[]> = {
      Todo: ['InProgress', 'Done'],
      InProgress: ['Done', 'Todo'],
      Done: ['Todo', 'InProgress'],
    };
    return order[task.viewerStatus].map((value) => ({ value, label: statusLabel(value) }));
  }

  protected icon(status: AgendaItemStatus): string {
    if (status === 'Done') return 'checkmark-circle';
    return status === 'InProgress' ? 'time-outline' : 'ellipse-outline';
  }

  protected targetsLine(task: AgendaItemDto): string {
    return task.assignments
      .map((a) => {
        const note = targetNote(a);
        return note ? `${a.label} · ${note}` : a.label;
      })
      .filter(Boolean)
      .join(', ');
  }

  /** A tap on the status mark: where to move the task. */
  protected async chooseStatus(task: AgendaItemDto): Promise<void> {
    if (!task.canChangeStatus) return;
    const choice = await this.pick(
      `«${task.title}»`,
      this.movesFor(task).map((to) => ({ text: to.label, data: to.value })),
    );
    if (choice) await this.move(task, choice as AgendaItemStatus);
  }

  protected async move(task: AgendaItemDto, status: AgendaItemStatus): Promise<void> {
    await this.closeSliding();
    if (this.moving()) return;
    this.moving.set(task.agendaItemKey);
    try {
      await this.agenda.changeStatus(task.agendaItemKey, status);
      if (status === 'Done') await this.toasts.show('Зроблено');
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося змінити статус.'), 'danger');
    } finally {
      this.moving.set(null);
      // Reloaded either way: a move can close a гурток's target and change every count.
      await this.load();
    }
  }

  protected async more(task: AgendaItemDto): Promise<void> {
    await this.closeSliding();
    const choice = await this.pick(`«${task.title}»`, [
      { text: 'Редагувати', data: 'edit' },
      { text: 'В архів', data: 'archive' },
    ]);
    if (choice === 'edit') await this.router.navigate(['/tabs/tasks/edit', task.agendaItemKey]);
    if (choice === 'archive') {
      try {
        await this.agenda.setArchived(task.agendaItemKey, true);
        await this.toasts.show('Перенесено в архів');
        await this.load();
      } catch (error) {
        await this.toasts.show(apiErrorText(error, 'Не вдалося перенести в архів.'), 'danger');
      }
    }
  }

  protected async remove(task: AgendaItemDto): Promise<void> {
    await this.closeSliding();
    if (!(await confirmAgendaDelete(this.sheets, task))) return;
    try {
      await this.agenda.delete(task.agendaItemKey);
      await this.toasts.show('Видалено');
      await this.load();
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося видалити.'), 'danger');
    }
  }

  /** An action sheet with a «Скасувати»; the chosen button's data, or undefined. */
  private async pick(header: string, buttons: ActionSheetButton[]): Promise<string | undefined> {
    const sheet = await this.sheets.create({ header, buttons: [...buttons, { text: 'Скасувати', role: 'cancel' }] });
    await sheet.present();
    const { data, role } = await sheet.onWillDismiss<string>();
    return role === 'cancel' || role === 'backdrop' ? undefined : data;
  }

  private async closeSliding(): Promise<void> {
    const list = this.host.querySelectorAll('ion-item-sliding');
    await Promise.all([...list].map((item) => (item as HTMLIonItemSlidingElement).close()));
  }
}

function toBoard(response: AgendaBoardResponse): Board {
  return {
    columns: STATUSES.map(({ value, label }) => {
      const column = response.columns.find((c) => c.status === value);
      return { status: value, label, total: column?.total ?? 0, items: column?.items ?? [] };
    }),
    targets: response.targets,
  };
}

/** A page that arrives after a move may repeat a card already shown; it is kept once. */
function append(column: Column, items: AgendaItemDto[], total: number): Column {
  const shown = new Set(column.items.map((i) => i.agendaItemKey));
  return { ...column, total, items: [...column.items, ...items.filter((i) => !shown.has(i.agendaItemKey))] };
}

function boardTargetLabel(target: AgendaBoardTarget): string {
  return target.targetType === 'Member' ? `${target.label} (особисто)` : target.label;
}
