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
  IonTitle,
  IonToolbar,
  ModalController,
} from '@ionic/angular';
import { apiErrorText } from '../../core/api';
import { Toasts } from '../../core/toast';
import { toDateOnly } from './kurin.labels';
import { MemberDto, OwnProfileForm } from './kurin.models';
import { KurinApi } from './kurin.service';

const SAVE_ERRORS = {
  ContactInfoLinked: 'Телефон цього акаунта змінюється в налаштуваннях акаунта.',
  EmailTaken: 'Ця адреса вже належить іншому акаунту.',
};

type Field = keyof OwnProfileForm;

const FIELDS: { key: Field; label: string; type: 'text' | 'tel' | 'date'; autocomplete: string }[] = [
  { key: 'firstName', label: 'Імʼя', type: 'text', autocomplete: 'given-name' },
  { key: 'middleName', label: 'По батькові', type: 'text', autocomplete: 'additional-name' },
  { key: 'lastName', label: 'Прізвище', type: 'text', autocomplete: 'family-name' },
  { key: 'phoneNumber', label: 'Номер телефону', type: 'tel', autocomplete: 'tel' },
  { key: 'dateOfBirth', label: 'Дата народження', type: 'date', autocomplete: 'bday' },
];

/**
 * A card's basic fields (web upsert-member): name, phone and birth date, on one's own card or, for
 * провід and admins, on anyone's the server lets them change. The email is the account's; the photo,
 * ступені, verification and перестороги stay on the web for now. Dismisses with the saved card.
 */
@Component({
  selector: 'app-profile-edit-sheet',
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
  ],
  styles: `
    ion-item > div {
      width: 100%;
      padding: 10px 0;
    }
    .hint {
      margin: 0 20px;
      font-size: 13px;
      color: var(--lk-muted);
    }
  `,
  template: `
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-button (click)="close(null)">Скасувати</ion-button>
        </ion-buttons>
        <ion-title>{{ own ? 'Мій профіль' : 'Профіль учасника' }}</ion-title>
        <ion-buttons slot="end">
          <ion-button [strong]="true" [disabled]="!valid() || busy()" (click)="save()" data-testid="save-profile">Зберегти</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>
    <ion-content>
      <ion-list [inset]="true">
        <ion-list-header><ion-label>Основне</ion-label></ion-list-header>
        <ion-item-group>
          @for (field of fields; track field.key) {
            <ion-item>
              <div class="lk-field">
                <span class="lk-field__label">{{ field.label }}</span>
                <div class="lk-input-box" [class.lk-invalid]="touched() && !form()[field.key].trim()">
                  <ion-input
                    [type]="field.type"
                    [attr.data-testid]="'profile-' + field.key"
                    [attr.aria-label]="field.label"
                    [autocomplete]="$any(field.autocomplete)"
                    [max]="field.type === 'date' ? today : undefined"
                    [value]="form()[field.key]"
                    (ionInput)="set(field.key, $any($event).detail.value)"
                  />
                </div>
              </div>
            </ion-item>
          }
        </ion-item-group>
      </ion-list>
      @if (own) {
        <p class="hint">Email ({{ member?.email }}) змінюється в налаштуваннях акаунта. Фото, ступені й решту змінює провід у вебі.</p>
      } @else {
        <p class="hint">Email, фото, ступені, перевірку й перестороги поки змінюють у вебі.</p>
      }
    </ion-content>
  `,
})
export class ProfileEditSheet implements OnInit {
  private readonly data = inject(KurinApi);
  private readonly modals = inject(ModalController);
  private readonly toasts = inject(Toasts);

  // Set by ModalController's componentProps.
  member: MemberDto | null = null;
  /** One's own card, or somebody else's opened by провід or an admin. */
  own = true;

  protected readonly fields = FIELDS;
  protected readonly today = toDateOnly(new Date());
  protected readonly form = signal<OwnProfileForm>({ firstName: '', middleName: '', lastName: '', phoneNumber: '', dateOfBirth: '' });
  protected readonly touched = signal(false);
  protected readonly busy = signal(false);
  protected readonly valid = computed(() => {
    const f = this.form();
    return FIELDS.every(({ key }) => f[key].trim().length > 0) && /^\d{4}-\d{2}-\d{2}$/.test(f.dateOfBirth) && f.dateOfBirth <= this.today;
  });

  ngOnInit(): void {
    const m = this.member;
    if (!m) return;
    this.form.set({
      firstName: m.firstName ?? '',
      middleName: m.middleName ?? '',
      lastName: m.lastName ?? '',
      phoneNumber: m.phoneNumber ?? '',
      dateOfBirth: (m.dateOfBirth ?? '').slice(0, 10),
    });
  }

  protected set(key: Field, value: string | null | undefined): void {
    this.touched.set(true);
    this.form.update((form) => ({ ...form, [key]: value ?? '' }));
  }

  protected close(saved: MemberDto | null): void {
    void this.modals.dismiss(saved);
  }

  protected async save(): Promise<void> {
    const member = this.member;
    if (!member || !this.valid() || this.busy()) return;
    this.busy.set(true);
    const f = this.form();
    try {
      const saved = await this.data.updateOwnProfile(member, {
        firstName: f.firstName.trim(),
        middleName: f.middleName.trim(),
        lastName: f.lastName.trim(),
        phoneNumber: f.phoneNumber.trim(),
        dateOfBirth: f.dateOfBirth,
      });
      await this.toasts.show('Профіль збережено.');
      this.close(saved);
    } catch (error) {
      await this.toasts.show(apiErrorText(error, 'Не вдалося зберегти профіль. Спробуй ще раз.', SAVE_ERRORS), 'danger');
    } finally {
      this.busy.set(false);
    }
  }
}
