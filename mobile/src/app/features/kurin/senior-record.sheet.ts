import { Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  ActionSheetController,
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonInput,
  IonItem,
  IonItemGroup,
  IonList,
  IonSelect,
  IonSelectOption,
  IonSpinner,
  IonTextarea,
  IonTitle,
  IonToolbar,
  ModalController,
} from '@ionic/angular';
import { apiErrorText } from '../../core/api';
import { Toasts } from '../../core/toast';
import { BOOK_KINDS, BOOK_SECTION_TITLES, toDateOnly } from './kurin.labels';
import { MemberSeniorRecordDto, SeniorCatalogItem, SeniorRecordKind } from './kurin.models';
import { KurinApi } from './kurin.service';

/**
 * A line of one's own книжечка (web member-senior-book-tile): what it is — first the section, then
 * the ступінь, відзначення or вишкіл —, a вишкіл's number, the date and a note. Only the owner
 * opens it. Dismisses with `true` after any change.
 */
@Component({
  selector: 'app-senior-record-sheet',
  imports: [
    IonHeader,
    IonToolbar,
    IonTitle,
    IonButtons,
    IonButton,
    IonContent,
    IonList,
    IonItem,
    IonItemGroup,
    IonSelect,
    IonSelectOption,
    IonInput,
    IonTextarea,
    IonSpinner,
  ],
  styles: `
    ion-item > div {
      width: 100%;
      padding: 10px 0;
    }
    .actions {
      padding: 4px 16px 24px;
      display: grid;
      gap: 8px;
    }
    .lk-input-box ion-textarea,
    .lk-input-box ion-input {
      --padding-start: 12px;
      --padding-end: 12px;
      font-size: 16px;
    }
  `,
  template: `
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-button (click)="close(false)">Скасувати</ion-button>
        </ion-buttons>
        <ion-title>{{ record ? 'Змінити' : 'До книжечки' }}</ion-title>
        <ion-buttons slot="end">
          <ion-button [strong]="true" [disabled]="!catalogId() || busy()" (click)="save()" data-testid="save-senior-record">Зберегти</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>
    <ion-content>
      <ion-list [inset]="true">
        <ion-item-group>
          <ion-item>
            <ion-select
              label="Розділ"
              interface="action-sheet"
              cancelText="Скасувати"
              [value]="kind()"
              (ionChange)="pickKind($any($event).detail.value)"
              data-testid="senior-record-kind"
            >
              @for (option of kinds; track option) {
                <ion-select-option [value]="option">{{ titles[option] }}</ion-select-option>
              }
            </ion-select>
          </ion-item>
          <ion-item>
            <ion-select
              label="Що"
              interface="alert"
              cancelText="Скасувати"
              okText="Обрати"
              placeholder="Обери"
              [value]="catalogId()"
              [disabled]="!choices().length"
              (ionChange)="catalogId.set($any($event).detail.value)"
              data-testid="senior-record-item"
            >
              @for (item of choices(); track item.id) {
                <ion-select-option [value]="item.id">{{ item.title }}</ion-select-option>
              }
            </ion-select>
          </ion-item>
          @if (kind() === 'Training') {
            <ion-item>
              <div class="lk-field">
                <span class="lk-field__label">Номер</span>
                <div class="lk-input-box">
                  <ion-input aria-label="Номер" placeholder="ЛШ 41" [maxlength]="50" [value]="number()" (ionInput)="number.set($any($event).detail.value ?? '')" />
                </div>
              </div>
            </ion-item>
          }
          <ion-item>
            <div class="lk-field">
              <span class="lk-field__label">Дата</span>
              <div class="lk-input-box">
                <ion-input type="date" aria-label="Дата" [max]="today" [value]="date()" (ionInput)="date.set($any($event).detail.value ?? '')" />
              </div>
            </div>
          </ion-item>
          <ion-item>
            <div class="lk-field">
              <span class="lk-field__label">Нотатка</span>
              <div class="lk-input-box">
                <ion-textarea aria-label="Нотатка" [autoGrow]="true" [rows]="2" [maxlength]="500" [value]="note()" (ionInput)="note.set($any($event).detail.value ?? '')" />
              </div>
            </div>
          </ion-item>
        </ion-item-group>
      </ion-list>

      @if (record) {
        <div class="actions">
          <ion-button expand="block" fill="clear" color="danger" class="lk-danger" [disabled]="busy()" (click)="remove()">
            @if (busy()) { <ion-spinner name="crescent" /> } @else { Видалити }
          </ion-button>
        </div>
      }
    </ion-content>
  `,
})
export class SeniorRecordSheet implements OnInit {
  private readonly data = inject(KurinApi);
  private readonly modals = inject(ModalController);
  private readonly sheets = inject(ActionSheetController);
  private readonly toasts = inject(Toasts);

  // Set by ModalController's componentProps.
  record: MemberSeniorRecordDto | null = null;
  catalog: SeniorCatalogItem[] = [];

  protected readonly kinds = BOOK_KINDS;
  protected readonly titles = BOOK_SECTION_TITLES;
  protected readonly today = toDateOnly(new Date());
  protected readonly kind = signal<SeniorRecordKind>('Degree');
  protected readonly catalogId = signal<string | null>(null);
  protected readonly number = signal('');
  protected readonly date = signal('');
  protected readonly note = signal('');
  protected readonly busy = signal(false);
  private readonly items = signal<SeniorCatalogItem[]>([]);
  protected readonly choices = computed(() => this.items().filter((item) => item.kind === this.kind()));

  ngOnInit(): void {
    this.items.set(this.catalog);
    const record = this.record;
    if (record) {
      this.kind.set(this.catalog.find((item) => item.id === record.catalogId)?.kind ?? 'Award');
      this.catalogId.set(record.catalogId);
      this.number.set(record.number ?? '');
      this.date.set(record.achievedOn?.slice(0, 10) ?? '');
      this.note.set(record.note ?? '');
    }
  }

  protected pickKind(kind: SeniorRecordKind): void {
    if (kind === this.kind()) return;
    this.kind.set(kind);
    this.catalogId.set(null);
  }

  protected close(changed: boolean): void {
    void this.modals.dismiss(changed);
  }

  protected async save(): Promise<void> {
    const catalogId = this.catalogId();
    if (!catalogId || this.busy()) return;
    const request = {
      catalogId,
      number: this.kind() === 'Training' ? this.number().trim() || null : null,
      achievedOn: /^\d{4}-\d{2}-\d{2}$/.test(this.date()) ? this.date() : null,
      note: this.note().trim() || null,
    };
    await this.run(() => this.data.saveSeniorRecord(request, this.record?.memberSeniorRecordKey), 'Збережено.', 'Не вдалося зберегти.');
  }

  protected async remove(): Promise<void> {
    const record = this.record;
    if (!record) return;
    const sheet = await this.sheets.create({
      header: 'Видалити з книжечки?',
      buttons: [
        { text: 'Видалити', role: 'destructive', data: 'remove' },
        { text: 'Скасувати', role: 'cancel' },
      ],
    });
    await sheet.present();
    const { data } = await sheet.onWillDismiss();
    if (data !== 'remove') return;
    await this.run(() => this.data.deleteSeniorRecord(record.memberSeniorRecordKey), 'Видалено.', 'Не вдалося видалити.');
  }

  private async run(action: () => Promise<unknown>, done: string, failed: string): Promise<void> {
    this.busy.set(true);
    try {
      await action();
      await this.toasts.show(done);
      this.close(true);
    } catch (error) {
      await this.toasts.show(apiErrorText(error, `${failed} Спробуй ще раз.`, { DegreeAlreadyRecorded: 'Цей ступінь уже є.' }), 'danger');
    } finally {
      this.busy.set(false);
    }
  }
}
