import { Component, OnInit, computed, inject, signal } from '@angular/core';
import {
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonInput,
  IonItem,
  IonItemGroup,
  IonLabel,
  IonList,
  IonListHeader,
  IonTextarea,
  IonTitle,
  IonToolbar,
  ModalController,
} from '@ionic/angular';
import { apiErrorText } from '../../core/api';
import { Toasts } from '../../core/toast';
import {
  KURIN_LIMITS,
  KurinProfileDraft,
  kurinProfileChanged,
  kurinProfileDraft,
  kurinProfileRequest,
  kurinProfileValid,
} from './kurin-profile';
import { KurinDto } from './kurin.models';
import { KurinApi } from './kurin.service';
import { kurinTitle } from '../../core/kurin-label';

/** The API's codes for a field past its limit, which the form already keeps it from. */
const KURIN_ERRORS = {
  StanytsiaTooLong: 'Станиця — не довше за 120 символів.',
  RegionOrCountryTooLong: 'Край — не довше за 120 символів.',
  NamedAfterTooLong: '«Імені» — не довше за 200 символів.',
  DescriptionTooLong: 'Опис — не довше за 4000 символів.',
};

/**
 * The kurin's own record (web kurin-panel «Редагування даних куреня»): імені, станиця, край and
 * the description. The Звʼязковий's (web canManageGroups). Closes with the saved record, or null.
 */
@Component({
  selector: 'app-kurin-edit-sheet',
  imports: [
    IonHeader,
    IonToolbar,
    IonTitle,
    IonButtons,
    IonButton,
    IonContent,
    IonList,
    IonListHeader,
    IonItem,
    IonItemGroup,
    IonLabel,
    IonInput,
    IonTextarea,
  ],
  styles: `
    ion-item > div {
      width: 100%;
      padding: 10px 0;
    }
    .lk-input-box ion-textarea {
      --padding-start: 12px;
      --padding-end: 12px;
      font-size: 16px;
    }
  `,
  template: `
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-button [disabled]="busy()" (click)="close(null)">Скасувати</ion-button>
        </ion-buttons>
        <ion-title>Дані куреня</ion-title>
        <ion-buttons slot="end">
          <ion-button [strong]="true" [disabled]="!valid() || busy()" (click)="save()" data-testid="save-kurin">Зберегти</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>
    <ion-content>
      <ion-list [inset]="true">
        <ion-list-header><ion-label>{{ title(kurin.number) }}</ion-label></ion-list-header>
        <ion-item-group>
          @for (field of fields; track field.key) {
            <ion-item>
              <div class="lk-field">
                <span class="lk-field__label">{{ field.label }}</span>
                <div class="lk-input-box" [class.lk-invalid]="draft()[field.key].trim().length > limits[field.key]">
                  <ion-input
                    [attr.aria-label]="field.label"
                    autocapitalize="sentences"
                    [maxlength]="limits[field.key]"
                    [value]="draft()[field.key]"
                    (ionInput)="set(field.key, $any($event).detail.value)"
                    [attr.data-testid]="'kurin-' + field.key"
                  />
                </div>
              </div>
            </ion-item>
          }
          <ion-item>
            <div class="lk-field">
              <span class="lk-field__label">Опис</span>
              <div class="lk-input-box" [class.lk-invalid]="draft().description.trim().length > limits.description">
                <ion-textarea
                  aria-label="Опис"
                  autocapitalize="sentences"
                  [autoGrow]="true"
                  [rows]="5"
                  [counter]="true"
                  [maxlength]="limits.description"
                  [value]="draft().description"
                  (ionInput)="set('description', $any($event).detail.value)"
                  data-testid="kurin-description"
                />
              </div>
            </div>
          </ion-item>
        </ion-item-group>
      </ion-list>
    </ion-content>
  `,
})
export class KurinEditSheet implements OnInit {
  protected readonly title = kurinTitle;
  private readonly data = inject(KurinApi);
  private readonly modals = inject(ModalController);
  private readonly toasts = inject(Toasts);

  // Set by ModalController's componentProps.
  kurin!: KurinDto;

  /** In the web form's order and words. */
  protected readonly fields = [
    { key: 'namedAfter', label: 'Імені / в честь кого' },
    { key: 'stanytsia', label: 'Станиця' },
    { key: 'regionOrCountry', label: 'Край' },
  ] as const;
  protected readonly limits = KURIN_LIMITS;
  protected readonly draft = signal<KurinProfileDraft>({ namedAfter: '', stanytsia: '', regionOrCountry: '', description: '' });
  protected readonly busy = signal(false);
  protected readonly valid = computed(() => kurinProfileValid(this.draft()));

  ngOnInit(): void {
    this.draft.set(kurinProfileDraft(this.kurin));
  }

  protected set(field: keyof KurinProfileDraft, value: string | null | undefined): void {
    this.draft.update((draft) => ({ ...draft, [field]: value ?? '' }));
  }

  protected close(saved: KurinDto | null): void {
    void this.modals.dismiss(saved);
  }

  protected async save(): Promise<void> {
    if (!this.valid() || this.busy()) return;
    if (!kurinProfileChanged(this.kurin, this.draft())) {
      this.close(null);
      return;
    }
    this.busy.set(true);
    try {
      const saved = await this.data.updateKurin(kurinProfileRequest(this.kurin, this.draft()));
      await this.toasts.show('Дані куреня збережено.');
      this.close(saved);
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося зберегти. Спробуй ще раз.', KURIN_ERRORS), 'danger');
    } finally {
      this.busy.set(false);
    }
  }
}
