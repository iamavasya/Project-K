import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  IonButton,
  IonContent,
  IonInput,
  IonList,
  IonItem,
  IonSpinner,
  IonText,
} from '@ionic/angular';
import { AuthService, loginErrorText } from '../auth/auth.service';

type Step = 'password' | 'code';

@Component({
  selector: 'app-login',
  imports: [IonContent, IonList, IonItem, IonInput, IonButton, IonSpinner, IonText],
  styles: `
    .wrap {
      max-width: 420px;
      margin: 0 auto;
      padding: calc(env(safe-area-inset-top) + 48px) 0 32px;
    }
    .brand {
      text-align: center;
      margin-bottom: 24px;
    }
    .brand img {
      width: 88px;
      height: 88px;
    }
    .brand h1 {
      margin: 12px 0 4px;
      font-size: 28px;
      font-weight: 700;
    }
    .brand p {
      margin: 0;
      color: var(--ion-color-medium);
    }
    .actions {
      padding: 8px 16px 0;
    }
    .error {
      display: block;
      padding: 0 20px 8px;
      font-size: 14px;
    }
    .links {
      display: flex;
      justify-content: space-between;
      padding: 16px 20px 0;
      font-size: 15px;
    }
  `,
  template: `
    <ion-content>
      <div class="wrap">
        <div class="brand">
          <img src="icons/icon.svg" alt="" />
          <h1>Лілейка</h1>
          <p>{{ step() === 'password' ? 'Увійди у свій акаунт' : 'Підтвердження входу' }}</p>
        </div>

        <form (submit)="$event.preventDefault(); submit()">
          @if (step() === 'password') {
            <ion-list [inset]="true">
              <ion-item>
                <ion-input
                  data-testid="email"
                  label="Email"
                  labelPlacement="stacked"
                  type="email"
                  inputmode="email"
                  autocomplete="username"
                  autocapitalize="off"
                  [value]="email()"
                  (ionInput)="email.set(text($event))"
                />
              </ion-item>
              <ion-item>
                <ion-input
                  data-testid="password"
                  label="Пароль"
                  labelPlacement="stacked"
                  type="password"
                  autocomplete="current-password"
                  [value]="password()"
                  (ionInput)="password.set(text($event))"
                />
              </ion-item>
            </ion-list>
          } @else {
            <ion-list [inset]="true">
              <ion-item>
                <ion-input
                  data-testid="code"
                  [label]="useRecoveryCode() ? 'Код відновлення' : 'Код з застосунку-автентифікатора'"
                  labelPlacement="stacked"
                  [type]="useRecoveryCode() ? 'text' : 'tel'"
                  [inputmode]="useRecoveryCode() ? 'text' : 'numeric'"
                  autocomplete="one-time-code"
                  [maxlength]="useRecoveryCode() ? 32 : 6"
                  [value]="code()"
                  (ionInput)="code.set(text($event))"
                />
              </ion-item>
            </ion-list>
          }

          @if (error(); as message) {
            <ion-text color="danger" class="error" role="alert">{{ message }}</ion-text>
          }

          <div class="actions">
            <ion-button type="submit" expand="block" [disabled]="!canSubmit()">
              @if (busy()) {
                <ion-spinner name="crescent" />
              } @else {
                {{ step() === 'password' ? 'Увійти' : 'Підтвердити' }}
              }
            </ion-button>
          </div>
        </form>

        <div class="links">
          @if (step() === 'password') {
            <a href="/forgot-password">Забули пароль?</a>
            <a href="/join">Подати заявку</a>
          } @else {
            <a href="" (click)="$event.preventDefault(); back()">Назад</a>
            <a href="" (click)="$event.preventDefault(); toggleRecovery()">
              {{ useRecoveryCode() ? 'Код з застосунку' : 'Код відновлення' }}
            </a>
          }
        </div>
      </div>
    </ion-content>
  `,
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  protected readonly step = signal<Step>('password');
  protected readonly email = signal('');
  protected readonly password = signal('');
  protected readonly code = signal('');
  protected readonly useRecoveryCode = signal(false);
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  private mfaToken: string | null = null;

  protected readonly canSubmit = computed(() => {
    if (this.busy()) return false;
    return this.step() === 'password'
      ? this.email().trim().length > 0 && this.password().length > 0
      : this.code().trim().length > 0;
  });

  protected text(event: Event): string {
    return String((event as CustomEvent<{ value?: string | null }>).detail.value ?? '');
  }

  protected async submit(): Promise<void> {
    if (!this.canSubmit()) return;
    this.busy.set(true);
    this.error.set(null);
    try {
      if (this.step() === 'password') {
        const outcome = await this.auth.login(this.email().trim(), this.password());
        if (outcome.kind === 'mfa') {
          this.mfaToken = outcome.mfaToken;
          this.step.set('code');
          return;
        }
      } else {
        await this.auth.verifyMfa(this.email().trim(), this.code().trim(), this.mfaToken);
      }
      await this.router.navigateByUrl('/tabs/home', { replaceUrl: true });
    } catch (error) {
      const fallback = this.step() === 'password' ? 'Не вдалося увійти.' : 'Невірний код підтвердження.';
      this.error.set(loginErrorText(error, fallback));
    } finally {
      this.busy.set(false);
    }
  }

  protected back(): void {
    this.step.set('password');
    this.code.set('');
    this.useRecoveryCode.set(false);
    this.error.set(null);
    this.mfaToken = null;
  }

  protected toggleRecovery(): void {
    this.useRecoveryCode.update((v) => !v);
    this.code.set('');
  }
}
