import { Component, OnInit, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import {
  IonBackButton,
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonInput,
  IonItem,
  IonItemGroup,
  IonList,
  IonListHeader,
  IonSpinner,
  IonTitle,
  IonToolbar,
  ToastController,
} from '@ionic/angular';
import { MfaSetup } from '../auth/auth.models';
import { AuthService, isOffline } from '../auth/auth.service';

type Step = 'loading' | 'failed' | 'already-on' | 'setup' | 'codes';

/**
 * Turning on the second factor. Mandatory for admins and the kurin's провід (mfaSetupGuard sends
 * them here and there is no way past it but signing out); anyone else gets here from «Ще».
 * On a phone the authenticator usually sits on the same device, so the otpauth link and the key
 * come first and the QR code is for setting up another device.
 */
@Component({
  selector: 'app-mfa-setup',
  imports: [
    IonHeader,
    IonToolbar,
    IonButtons,
    IonBackButton,
    IonTitle,
    IonContent,
    IonList,
    IonListHeader,
    IonItem,
    IonItemGroup,
    IonInput,
    IonButton,
    IonSpinner,
  ],
  styles: `
    .wrap {
      max-width: 520px;
      margin: 0 auto;
      padding-bottom: 32px;
    }
    .lead {
      padding: 8px 20px 0;
    }
    .lead h2 {
      font-size: 22px;
      font-weight: 700;
      margin: 8px 0;
    }
    .lead p {
      margin: 0 0 8px;
      color: var(--lk-muted);
    }
    .actions {
      padding: 4px 16px;
    }
    ion-item > div {
      padding: 12px 0;
      width: 100%;
    }
    .hint {
      margin: 0 0 8px;
      font-size: 14px;
      color: var(--lk-muted);
    }
    .key {
      font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
      word-break: break-all;
      font-size: 15px;
    }
    .qr {
      display: flex;
      justify-content: center;
      padding: 8px 0 16px;
    }
    .qr img {
      width: 200px;
      height: 200px;
      background: #fff;
      padding: 8px;
      border-radius: 12px;
    }
    .codes {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 8px;
      padding: 8px 20px 16px;
      font-family: ui-monospace, SFMono-Regular, Menlo, monospace;
      font-size: 16px;
    }
    .center {
      display: flex;
      justify-content: center;
      padding: 48px;
    }
  `,
  template: `
    <ion-header [translucent]="true">
      <ion-toolbar>
        @if (!mandatory()) {
          <ion-buttons slot="start"><ion-back-button defaultHref="/tabs/more" text="Ще" /></ion-buttons>
        }
        <ion-title>Двофакторний вхід</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content>
      <div class="wrap">
        @switch (step()) {
          @case ('loading') {
            <div class="center"><ion-spinner name="crescent" /></div>
          }
          @case ('failed') {
            <div class="lead">
              <h2>Не вдалося почати</h2>
              <p>{{ failedText() }}</p>
            </div>
            <div class="actions"><ion-button expand="block" (click)="load()">Спробувати ще раз</ion-button></div>
          }
          @case ('already-on') {
            <div class="lead">
              <h2>Уже увімкнено</h2>
              <p>Під час входу Лілейка питатиме код із застосунку-автентифікатора.</p>
            </div>
            <div class="actions"><ion-button expand="block" (click)="done()">На головну</ion-button></div>
          }
          @case ('setup') {
            <div class="lead">
              <h2>Захисти акаунт</h2>
              @if (mandatory()) {
                <p>Для проводу і адміністраторів двофакторний вхід обовʼязковий. Увімкни його, щоб продовжити.</p>
              } @else {
                <p>Окрім пароля, під час входу Лілейка питатиме код із застосунку-автентифікатора.</p>
              }
            </div>

            <ion-list [inset]="true">
              <ion-list-header>1. Додай Лілейку в застосунок</ion-list-header>
              <ion-item-group>
                <ion-item>
                  <div>
                    <p class="hint">Google Authenticator, 1Password або інший. Якщо він на цьому телефоні, відкрий його кнопкою.</p>
                    <ion-button expand="block" [href]="setup()?.authenticatorUri">Відкрити застосунок-автентифікатор</ion-button>
                  </div>
                </ion-item>
                <ion-item>
                  <div>
                    <p class="hint">Або введи ключ вручну:</p>
                    <p class="key" data-testid="shared-key">{{ setup()?.sharedKey }}</p>
                    <ion-button size="small" fill="outline" (click)="copy(setup()?.sharedKey ?? '', 'Ключ скопійовано')">
                      Скопіювати ключ
                    </ion-button>
                    <ion-button size="small" fill="clear" (click)="showQr.set(!showQr())">
                      {{ showQr() ? 'Сховати QR-код' : 'QR-код для іншого пристрою' }}
                    </ion-button>
                  </div>
                </ion-item>
              </ion-item-group>
            </ion-list>
            @if (showQr()) {
              <div class="qr"><img [src]="setup()?.qrCodeBase64" alt="QR-код для застосунку-автентифікатора" /></div>
            }

            <form (submit)="$event.preventDefault(); enable()">
              <ion-list [inset]="true">
                <ion-list-header>2. Введи код із застосунку</ion-list-header>
                <ion-item-group>
                  <ion-item>
                    <div class="lk-field">
                      <span class="lk-field__label">Шестизначний код</span>
                      <div class="lk-input-box" [class.lk-invalid]="!!error()">
                        <ion-input
                          data-testid="mfa-code"
                          aria-label="Шестизначний код"
                          type="tel"
                          inputmode="numeric"
                          autocomplete="one-time-code"
                          [maxlength]="6"
                          [value]="code()"
                          (ionInput)="code.set(digits($event))"
                        />
                      </div>
                      @if (error(); as message) {
                        <span class="lk-field__error" role="alert">{{ message }}</span>
                      }
                    </div>
                  </ion-item>
                </ion-item-group>
              </ion-list>
              <div class="actions">
                <ion-button type="submit" expand="block" [disabled]="code().length !== 6 || busy()">
                  @if (busy()) { <ion-spinner name="crescent" /> } @else { Увімкнути }
                </ion-button>
              </div>
            </form>
          }
          @case ('codes') {
            <div class="lead">
              <h2>Збережи резервні коди</h2>
              <p>Кожен спрацьовує один раз, якщо застосунок-автентифікатор буде недоступний. Збережи їх у надійному місці.</p>
            </div>
            <div class="codes" data-testid="recovery-codes">
              @for (item of recoveryCodes(); track item) { <span>{{ item }}</span> }
            </div>
            <div class="actions">
              <ion-button expand="block" fill="outline" (click)="copyCodes()">
                Скопіювати коди
              </ion-button>
              <ion-button expand="block" (click)="done()">Я зберіг коди</ion-button>
            </div>
          }
        }

        @if (mandatory() && step() !== 'codes') {
          <div class="actions">
            <ion-button expand="block" fill="clear" color="medium" (click)="signOut()">Вийти</ion-button>
          </div>
        }
      </div>
    </ion-content>
  `,
})
export class MfaSetupPage implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly toasts = inject(ToastController);

  protected readonly step = signal<Step>('loading');
  protected readonly mandatory = signal(false);
  protected readonly setup = signal<MfaSetup | null>(null);
  protected readonly showQr = signal(false);
  protected readonly code = signal('');
  protected readonly busy = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly failedText = signal('');
  protected readonly recoveryCodes = signal<string[]>([]);

  ngOnInit(): void {
    void this.load();
  }

  protected async load(): Promise<void> {
    this.step.set('loading');
    try {
      const status = await this.auth.mfaStatus();
      this.mandatory.set(status.isMfaRequired && !status.isMfaEnabled);
      if (status.isMfaEnabled) {
        this.step.set('already-on');
        return;
      }
      this.setup.set(await this.auth.mfaSetup());
      this.step.set('setup');
    } catch (error) {
      this.failedText.set(
        isOffline(error) ? 'Немає зв’язку з сервером. Перевір інтернет.' : 'Сервер не відповів як слід. Спробуй ще раз.',
      );
      this.step.set('failed');
    }
  }

  protected digits(event: Event): string {
    const value = String((event as CustomEvent<{ value?: string | null }>).detail.value ?? '');
    return value.replace(/\D/g, '').slice(0, 6);
  }

  protected async enable(): Promise<void> {
    if (this.code().length !== 6 || this.busy()) return;
    this.busy.set(true);
    this.error.set(null);
    try {
      this.recoveryCodes.set(await this.auth.enableMfa(this.code()));
      this.step.set('codes');
    } catch (error) {
      this.error.set(
        isOffline(error)
          ? 'Немає зв’язку з сервером. Перевір інтернет.'
          : 'Код не підійшов. Перевір застосунок і спробуй ще раз.',
      );
    } finally {
      this.busy.set(false);
    }
  }

  protected async copy(text: string, done: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(text);
      await this.toast(done);
    } catch {
      await this.toast('Не вдалося скопіювати. Виділи текст вручну.');
    }
  }

  protected copyCodes(): Promise<void> {
    return this.copy(this.recoveryCodes().join('\n'), 'Коди скопійовано');
  }

  protected async done(): Promise<void> {
    await this.router.navigateByUrl('/tabs/home', { replaceUrl: true });
  }

  protected async signOut(): Promise<void> {
    await this.auth.logout();
    await this.router.navigateByUrl('/login', { replaceUrl: true });
  }

  private async toast(message: string): Promise<void> {
    const toast = await this.toasts.create({ message, duration: 2000, position: 'top' });
    await toast.present();
  }
}
