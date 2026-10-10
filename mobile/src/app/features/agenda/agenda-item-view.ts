import { Component, ElementRef, OnDestroy, afterRenderEffect, computed, effect, inject, input, output, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  ActionSheetController,
  IonIcon,
  IonItem,
  IonItemGroup,
  IonLabel,
  IonList,
  IonListHeader,
  IonNote,
  IonSegment,
  IonSegmentButton,
  IonSkeletonText,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { archiveOutline, locationOutline, peopleOutline, repeatOutline, timeOutline, trophy } from 'ionicons/icons';
import { AuthService } from '../../auth/auth.service';
import { apiErrorText } from '../../core/api';
import { FAILED_TEXT, Loaded, valueOf } from '../../core/loaded';
import { Toasts } from '../../core/toast';
import { GlassEffects } from '../../ui/glass';
import { AgendaProgress } from './agenda-progress';
import { AgendaItemDto, AgendaResponsesResponse, AgendaRsvpDto, AgendaRsvpStatus } from './agenda.models';
import {
  RSVP_OPTIONS,
  canScore,
  occurrenceOf,
  recurrenceLabel,
  scheduleOf,
  targetsLabel,
  whenLines,
} from './agenda.labels';
import { AgendaService } from './agenda.service';

/**
 * An event or a task, read-only, with what the viewer may do on it (the web's agenda-item-dialog in
 * its view mode): the answer to an event with everyone else's, a task's progress, the attendance
 * sheet for those who score, and archive or delete for those the API lets edit it.
 */
@Component({
  selector: 'app-agenda-item-view',
  imports: [
    RouterLink,
    IonList,
    IonListHeader,
    IonItemGroup,
    IonItem,
    IonLabel,
    IonNote,
    IonIcon,
    IonSegment,
    IonSegmentButton,
    IonSkeletonText,
    AgendaProgress,
  ],
  styles: `
    .head {
      padding: 8px 20px 4px;
    }
    :host-context(.ios) .head {
      padding-inline: 16px;
    }
    h1 {
      margin: 0 0 6px;
      font-size: 24px;
      line-height: 30px;
      font-weight: 700;
      color: var(--lk-ink);
    }
    .chips {
      display: flex;
      flex-wrap: wrap;
      gap: 6px 12px;
      color: var(--lk-muted);
      font-size: 14px;
    }
    .dot {
      display: inline-block;
      width: 10px;
      height: 10px;
      border-radius: 50%;
      margin-right: 6px;
      background: var(--lk-primary);
    }
    .row-icon {
      color: var(--lk-muted);
      font-size: 20px;
    }
    .desc {
      white-space: pre-wrap;
    }
    .counts {
      display: grid;
      grid-template-columns: repeat(3, 1fr);
      text-align: center;
      padding: 10px 0;
      width: 100%;
    }
    .counts strong {
      display: block;
      font-size: 22px;
      color: var(--lk-ink);
    }
    .counts span {
      font-size: 13px;
      color: var(--lk-muted);
    }
    .rsvp ion-segment {
      margin: 8px 0;
      width: 100%;
    }
    .meta {
      margin: 0 20px 16px;
      font-size: 13px;
      color: var(--lk-muted);
    }
    :host-context(.ios) .meta {
      margin-inline: 32px;
    }
    .center {
      text-align: center;
    }
  `,
  template: `
    @switch (state().state) {
      @case ('loading') {
        <div class="head"><ion-skeleton-text [animated]="true" style="width: 70%; height: 28px" /></div>
        <ion-list [inset]="true">
          <ion-item-group>
            <ion-item><ion-skeleton-text [animated]="true" style="height: 40px" /></ion-item>
            <ion-item><ion-skeleton-text [animated]="true" style="height: 40px" /></ion-item>
          </ion-item-group>
        </ion-list>
      }
      @case ('failed') {
        <ion-list [inset]="true">
          <ion-item-group>
            <ion-item><ion-label class="ion-text-wrap">{{ failedText }}</ion-label></ion-item>
          </ion-item-group>
        </ion-list>
      }
      @default {
        @if (item(); as it) {
          <div class="head">
            <h1 data-testid="item-title">{{ it.title }}</h1>
            <div class="chips">
              @if (it.categoryName) {
                <span><i class="dot" [style.background]="it.categoryColorHex"></i>{{ it.categoryName }}</span>
              }
              @if (schedule(); as whose) { <span>Графік куреня · {{ whose }}</span> }
              @if (!it.addressedToViewer) { <span>Призначено іншим</span> }
            </div>
          </div>

          <ion-list [inset]="true">
            <ion-item-group>
              @if (when().length) {
                <ion-item>
                  <ion-icon slot="start" class="row-icon" name="time-outline" aria-hidden="true" />
                  <ion-label class="ion-text-wrap">
                    <h3>{{ when()[0] }}</h3>
                    <p>{{ when()[1] }}</p>
                  </ion-label>
                </ion-item>
              }
              @if (repeats()) {
                <ion-item>
                  <ion-icon slot="start" class="row-icon" name="repeat-outline" aria-hidden="true" />
                  <ion-label class="ion-text-wrap">{{ repeats() }}</ion-label>
                </ion-item>
              }
              @if (it.location) {
                <ion-item>
                  <ion-icon slot="start" class="row-icon" name="location-outline" aria-hidden="true" />
                  <ion-label class="ion-text-wrap">{{ it.location }}</ion-label>
                </ion-item>
              }
              @if (it.kind === 'Event' && targets()) {
                <ion-item>
                  <ion-icon slot="start" class="row-icon" name="people-outline" aria-hidden="true" />
                  <ion-label class="ion-text-wrap">{{ targets() }}</ion-label>
                </ion-item>
              }
            </ion-item-group>
          </ion-list>

          @if (it.description) {
            <ion-list [inset]="true">
              <ion-list-header><ion-label>Опис</ion-label></ion-list-header>
              <ion-item-group>
                <ion-item><ion-label class="ion-text-wrap desc">{{ it.description }}</ion-label></ion-item>
              </ion-item-group>
            </ion-list>
          }

          @if (it.kind === 'Task' && it.assignments.length) {
            <app-agenda-progress [item]="it" (changed)="load()" />
          }

          @if (rsvpValue(); as picture) {
            <ion-list [inset]="true" class="rsvp" data-testid="rsvp">
              <ion-list-header><ion-label>Твоя відповідь</ion-label></ion-list-header>
              <ion-item-group>
                <ion-item>
                  <ion-segment [value]="picture.myStatus ?? ''" [disabled]="answering()" (ionChange)="respond($event)">
                    @for (option of rsvpOptions; track option.value) {
                      <ion-segment-button [value]="option.value"><ion-label>{{ option.label }}</ion-label></ion-segment-button>
                    }
                  </ion-segment>
                </ion-item>
                <ion-item>
                  <div class="counts" data-testid="rsvp-counts">
                    <div><strong>{{ picture.goingConfirmedCount }}</strong><span>йдуть</span></div>
                    <div><strong>{{ picture.maybeCount }}</strong><span>можливо</span></div>
                    <div><strong>{{ picture.notGoingCount }}</strong><span>не йдуть</span></div>
                  </div>
                </ion-item>
                @if (picture.goingWaitlistCount > 0) {
                  <ion-item><ion-label>У черзі</ion-label><ion-note slot="end">{{ picture.goingWaitlistCount }}</ion-note></ion-item>
                }
              </ion-item-group>
            </ion-list>
            @if (picture.responses.length) {
              <ion-list [inset]="true" data-testid="rsvp-list">
                <ion-list-header><ion-label>Хто відповів</ion-label></ion-list-header>
                <ion-item-group>
                  @for (answer of picture.responses; track answer.userKey) {
                    <ion-item>
                      <ion-label>{{ answer.displayName }}</ion-label>
                      <ion-note slot="end">{{ answerLabel(answer) }}</ion-note>
                    </ion-item>
                  }
                </ion-item-group>
              </ion-list>
            }
          }

          @if (sheetOpen()) {
            <ion-list [inset]="true">
              <ion-item-group>
                <ion-item
                  [button]="true"
                  [detail]="true"
                  [routerLink]="['/tabs/calendar/attendance', it.agendaItemKey]"
                  [queryParams]="{ start: it.startUtc }"
                  data-testid="attendance"
                >
                  <ion-icon class="lk-tile" slot="start" name="trophy" aria-hidden="true" style="--lk-tile: #e8a33d" />
                  <ion-label>Точкування</ion-label>
                </ion-item>
              </ion-item-group>
            </ion-list>
          }

          @if (it.createdByName) { <p class="meta">Автор: {{ it.createdByName }}</p> }

          @if (it.canEdit) {
            <ion-list [inset]="true">
              <ion-item-group>
                @if (it.kind === 'Task') {
                  <ion-item [button]="true" [detail]="false" [disabled]="busy()" (click)="archive()">
                    <ion-label class="center" color="primary">В архів</ion-label>
                  </ion-item>
                }
                <ion-item [button]="true" [detail]="false" [disabled]="busy()" (click)="confirmDelete()" data-testid="delete">
                  <ion-label class="center" color="danger">{{ it.kind === 'Task' ? 'Видалити задачу' : 'Видалити подію' }}</ion-label>
                </ion-item>
              </ion-item-group>
            </ion-list>
          }
        }
      }
    }
  `,
})
export class AgendaItemView implements OnDestroy {
  private readonly agenda = inject(AgendaService);
  private readonly auth = inject(AuthService);
  private readonly toasts = inject(Toasts);
  private readonly sheets = inject(ActionSheetController);
  private readonly glass = new GlassEffects(inject<ElementRef<HTMLElement>>(ElementRef).nativeElement);

  readonly itemKey = input.required<string>();
  /** The occurrence's start, for a series (and any calendar row): its answers are per occurrence. */
  readonly start = input<string | null>(null);
  /** Deleted or archived: the page goes back to its list. */
  readonly removed = output<void>();

  protected readonly state = signal<Loaded<AgendaItemDto>>({ state: 'loading' });
  readonly item = computed(() => valueOf(this.state()));
  protected readonly rsvp = signal<AgendaResponsesResponse | null>(null);
  protected readonly rsvpValue = this.rsvp.asReadonly();
  protected readonly answering = signal(false);
  protected readonly busy = signal(false);
  protected readonly failedText = FAILED_TEXT;
  protected readonly rsvpOptions = RSVP_OPTIONS;

  protected readonly when = computed(() => {
    const item = this.item();
    return item ? whenLines(item) : [];
  });
  protected readonly repeats = computed(() => {
    const item = this.item();
    return item ? recurrenceLabel(item) : '';
  });
  protected readonly targets = computed(() => {
    const item = this.item();
    return item ? targetsLabel(item) : '';
  });
  protected readonly schedule = computed(() => {
    const item = this.item();
    return item ? scheduleOf(item) : null;
  });
  /** The web's «Точкування»: a saved event with a day, for those who score; never a task. */
  protected readonly sheetOpen = computed(() => {
    const item = this.item();
    return !!item && item.kind === 'Event' && !!item.startUtc && canScore(this.auth.user());
  });

  constructor() {
    addIcons({ timeOutline, repeatOutline, locationOutline, peopleOutline, trophy, archiveOutline });
    effect(() => {
      this.itemKey();
      this.start();
      void this.load();
    });
    afterRenderEffect(() => {
      this.state();
      this.rsvp();
      this.glass.sync();
    });
  }

  ngOnDestroy(): void {
    this.glass.destroy();
  }

  /** Re-reads the item (and its answers); what was shown stays if the read fails. */
  async load(): Promise<void> {
    try {
      const item = await this.read();
      this.state.set({ state: 'ready', value: item });
      await this.loadResponses(item);
    } catch {
      if (this.state().state !== 'ready') this.state.set({ state: 'failed' });
    }
  }

  /**
   * A calendar row is found again in the calendar feed by its start: that carries the occurrence
   * (and an event seen only through «Графіки гуртків», which the item endpoint does not show).
   * Anything else is read by its key.
   */
  private async read(): Promise<AgendaItemDto> {
    const key = this.itemKey();
    const start = this.start();
    const kurinKey = this.auth.user()?.kurinKey;
    if (start && kurinKey) {
      try {
        const at = new Date(start).getTime();
        const rows = await this.agenda.calendar(kurinKey, start, start, true);
        const row = rows.find((r) => r.agendaItemKey === key && r.startUtc && new Date(r.startUtc).getTime() === at);
        if (row) return row;
      } catch {
        // Fall back to the item itself.
      }
    }
    return this.agenda.item(key);
  }

  /** Answers only for an event one is addressed by; a schedule-only event has nobody to answer to. */
  private async loadResponses(item: AgendaItemDto): Promise<void> {
    if (item.kind !== 'Event' || item.audience === 'Schedule') {
      this.rsvp.set(null);
      return;
    }
    try {
      this.rsvp.set(await this.agenda.responses(item.agendaItemKey, occurrenceOf(item)));
    } catch {
      this.rsvp.set(null);
    }
  }

  protected answerLabel(answer: AgendaRsvpDto): string {
    if (answer.status === 'Going') return answer.isWaitlisted ? 'у черзі' : 'йде';
    return answer.status === 'Maybe' ? 'можливо' : 'не йде';
  }

  protected async respond(change: Event): Promise<void> {
    const status = (change as CustomEvent<{ value?: string }>).detail.value as AgendaRsvpStatus | undefined;
    const item = this.item();
    const before = this.rsvp();
    if (!item || !status || status === before?.myStatus || this.answering()) return;
    this.answering.set(true);
    try {
      this.rsvp.set(await this.agenda.respond(item.agendaItemKey, status, occurrenceOf(item)));
    } catch (error) {
      // Back to what the server has, so the segment does not show an answer that was not saved.
      const segment = change.target as HTMLIonSegmentElement | null;
      if (segment) segment.value = before?.myStatus ?? '';
      await this.toasts.show(apiErrorText(error, 'Не вдалося зберегти відповідь.'), 'danger');
    } finally {
      this.answering.set(false);
    }
  }

  protected async archive(): Promise<void> {
    const item = this.item();
    if (!item) return;
    this.busy.set(true);
    try {
      await this.agenda.setArchived(item.agendaItemKey, true);
      await this.toasts.show('Перенесено в архів');
      this.removed.emit();
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося перенести в архів.'), 'danger');
    } finally {
      this.busy.set(false);
    }
  }

  /** As iOS Calendar: a sheet that names what goes, the whole series for a repeating event. */
  protected async confirmDelete(): Promise<void> {
    const item = this.item();
    if (!item) return;
    const confirmed = await confirmAgendaDelete(this.sheets, item);
    if (!confirmed) return;
    this.busy.set(true);
    try {
      await this.agenda.delete(item.agendaItemKey);
      await this.toasts.show('Видалено');
      this.removed.emit();
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося видалити.'), 'danger');
    } finally {
      this.busy.set(false);
    }
  }
}

/** The destructive sheet for deleting an item; true once the person confirms. */
export async function confirmAgendaDelete(sheets: ActionSheetController, item: AgendaItemDto): Promise<boolean> {
  const series = item.recurrenceFrequency !== 'None';
  const noun = item.kind === 'Task' ? 'задачу' : 'подію';
  const sheet = await sheets.create({
    header: series
      ? `«${item.title}» повторюється. Видалити всю серію?`
      : `Видалити ${noun} «${item.title}»? Її не можна буде повернути.`,
    buttons: [
      { text: series ? 'Видалити всі повторення' : `Видалити ${noun}`, role: 'destructive', data: 'delete' },
      { text: 'Скасувати', role: 'cancel' },
    ],
  });
  await sheet.present();
  const { data } = await sheet.onWillDismiss();
  return data === 'delete';
}
