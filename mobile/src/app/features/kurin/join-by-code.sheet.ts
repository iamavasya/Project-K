import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, inject, signal } from '@angular/core';
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
  IonNote,
  IonSpinner,
  IonTitle,
  IonToolbar,
  ModalController,
} from '@ionic/angular';
import { isOffline } from '../../auth/auth.service';
import { apiErrorText } from '../../core/api';
import { Toasts } from '../../core/toast';
import { MembershipCandidateDto } from './kurin.models';
import { KurinApi } from './kurin.service';
import { MemberAvatar } from './member-avatar';

const JOIN_ERRORS = {
  AlreadyAMember: 'Ця людина вже в цьому курені.',
  NoSuchCode: 'Такого коду ні в кого немає.',
};

/**
 * «Прийняти за кодом» (web join-by-code-dialog): someone already in the system joins this kurin by
 * the public code they handed over. Two steps on purpose, as on the web: first the card, then the
 * consent, so a mistyped character never quietly takes in someone else. Closes with true once
 * they are in.
 */
@Component({
  selector: 'app-join-by-code-sheet',
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
    IonNote,
    IonInput,
    IonSpinner,
    MemberAvatar,
  ],
  styles: `
    ion-item > div {
      width: 100%;
      padding: 10px 0;
    }
    .hint {
      margin: 0;
      padding: 8px 20px 0;
      color: var(--lk-muted);
      font-size: 14px;
      line-height: 20px;
    }
    .error {
      margin: 0;
      padding: 0 20px;
      color: var(--lk-danger);
      font-size: 14px;
    }
    .actions {
      padding: 8px 16px 32px;
    }
    app-member-avatar {
      margin-inline-end: 12px;
    }
    ion-spinner {
      width: 18px;
      height: 18px;
    }
  `,
  template: `
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-button [disabled]="joining()" (click)="close(false)">Скасувати</ion-button>
        </ion-buttons>
        <!-- Short: the row that opened it already says «Прийняти за кодом», and iPhone has room for little beside «Скасувати». -->
        <ion-title>За кодом</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content>
      <p class="hint">
        Код людина знаходить у власному профілі й передає проводу сама. Пошуку тут немає — лише точний збіг.
      </p>
      <ion-list [inset]="true">
        <ion-item-group>
          <ion-item>
            <div class="lk-field">
              <span class="lk-field__label">Публічний код людини</span>
              <div class="lk-input-box">
                <ion-input
                  aria-label="Публічний код людини"
                  placeholder="PL-XXXXX-XXXXX"
                  autocapitalize="characters"
                  autocomplete="off"
                  [spellcheck]="false"
                  enterkeyhint="search"
                  [value]="code()"
                  (ionInput)="typed($any($event).detail.value)"
                  (keyup.enter)="lookUp()"
                  data-testid="join-code"
                />
              </div>
            </div>
          </ion-item>
          <ion-item [button]="true" [detail]="false" [disabled]="!code().trim() || looking()" (click)="lookUp()" data-testid="join-find">
            <ion-label color="primary">Знайти</ion-label>
            @if (looking()) { <ion-spinner slot="end" name="crescent" /> }
          </ion-item>
        </ion-item-group>
      </ion-list>

      @if (error(); as message) {
        <p class="error" role="alert" data-testid="join-error">{{ message }}</p>
      }

      @if (candidate(); as found) {
        <ion-list [inset]="true" data-testid="join-candidate">
          <ion-list-header><ion-label>За цим кодом</ion-label></ion-list-header>
          <ion-item-group>
            <ion-item>
              <app-member-avatar slot="start" [firstName]="found.member.firstName" [lastName]="found.member.lastName" />
              <ion-label class="ion-text-wrap">
                <h3>{{ found.member.fullName }}</h3>
                <p>Належить до куренів: {{ found.member.currentMembershipCount }}</p>
              </ion-label>
            </ion-item>
            @if (found.alreadyInThisKurin) {
              <ion-item>
                <ion-note class="ion-text-wrap" data-testid="join-already">Ця людина вже в цьому курені.</ion-note>
              </ion-item>
            }
          </ion-item-group>
        </ion-list>
        <!-- The consent, under the card it is about: the second of the two steps. -->
        <div class="actions">
          <ion-button expand="block" [disabled]="!canJoin() || joining()" (click)="join()" data-testid="join-confirm">
            @if (joining()) { <ion-spinner name="crescent" /> } @else { Прийняти }
          </ion-button>
        </div>
      }
    </ion-content>
  `,
})
export class JoinByCodeSheet {
  private readonly data = inject(KurinApi);
  private readonly modals = inject(ModalController);
  private readonly toasts = inject(Toasts);

  // Set by ModalController's componentProps.
  kurinKey!: string;

  protected readonly code = signal('');
  protected readonly candidate = signal<MembershipCandidateDto | null>(null);
  protected readonly looking = signal(false);
  protected readonly joining = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly canJoin = computed(() => {
    const found = this.candidate();
    return !!found && !found.alreadyInThisKurin;
  });

  /** A changed code no longer names the card shown: it has to be looked up again. */
  protected typed(value: string | null | undefined): void {
    this.code.set(value ?? '');
    this.candidate.set(null);
    this.error.set(null);
  }

  protected async lookUp(): Promise<void> {
    const code = this.code().trim();
    if (!code || this.looking()) return;
    this.looking.set(true);
    this.candidate.set(null);
    this.error.set(null);
    try {
      this.candidate.set(await this.data.findCandidate(this.kurinKey, code));
    } catch (error) {
      // One answer for a wrong code and a code nobody has: the server does not tell them apart.
      const unknown = error instanceof HttpErrorResponse && !isOffline(error) && (error.status === 400 || error.status === 404);
      this.error.set(unknown ? JOIN_ERRORS.NoSuchCode : apiErrorText(error, 'Не вдалося перевірити код. Спробуй ще раз.'));
    } finally {
      this.looking.set(false);
    }
  }

  protected async join(): Promise<void> {
    const found = this.candidate();
    if (!found || found.alreadyInThisKurin || this.joining()) return;
    this.joining.set(true);
    this.error.set(null);
    try {
      await this.data.joinByPublicId(this.kurinKey, found.member.publicId);
      await this.toasts.show(`${found.member.fullName} тепер у курені.`);
      this.close(true);
    } catch (error) {
      this.error.set(apiErrorText(error, 'Не вдалося прийняти. Спробуй ще раз.', JOIN_ERRORS));
    } finally {
      this.joining.set(false);
    }
  }

  protected close(joined: boolean): void {
    void this.modals.dismiss(joined);
  }
}
