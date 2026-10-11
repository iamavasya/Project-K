import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  IonButton,
  IonContent,
  IonIcon,
  IonItem,
  IonItemGroup,
  IonList,
  IonSpinner,
} from '@ionic/angular';
import { addIcons } from 'ionicons';
import { checkmarkCircle, copyOutline, downloadOutline, logoChrome } from 'ionicons/icons';
import { Toasts } from '../core/toast';
import { fullVersionUrl, installHelpUrl, openExternal } from '../features/account/web-links';
import { InstallArt } from '../pwa/install-art';
import { canOfferPrompt, currentInstallFacts, emphasize, installGuide } from '../pwa/install-guide';
import { InstallService } from '../pwa/install.service';
import { manualInstall } from '../pwa/install-steps';

/** How long a Chromium tab may take to fire beforeinstallprompt before the steps by hand show. */
const PROMPT_WAIT_MS = 2500;

type View = 'installed' | 'done' | 'prompt' | 'waiting' | 'manual';

/**
 * /install, open without signing in: the web's «Встановити» banner and the docs send people here.
 * It works out what this browser can do: the system's install dialog in one tap (Chromium), the
 * Share-sheet steps with pictures (iPhone/iPad), «open it in Safari/Chrome» out of a messenger's
 * own browser, or the desktop's way; and says so when Лілейка already runs from the home screen.
 */
@Component({
  selector: 'app-install',
  imports: [
    IonContent,
    IonButton,
    IonIcon,
    IonSpinner,
    IonList,
    IonItemGroup,
    IonItem,
    RouterLink,
    InstallArt,
  ],
  styles: `
    .wrap {
      max-width: 520px;
      margin: 0 auto;
      padding: calc(env(safe-area-inset-top) + 32px) 0 calc(env(safe-area-inset-bottom) + 32px);
    }
    .brand {
      text-align: center;
      padding: 0 20px 16px;
    }
    .brand img {
      width: 72px;
      height: 72px;
    }
    .brand h1 {
      margin: 12px 0 6px;
      font-size: 26px;
      line-height: 32px;
      font-weight: 700;
    }
    .brand p,
    .lead p {
      margin: 0;
      color: var(--lk-muted);
      font-size: 15px;
      line-height: 21px;
    }
    .lead {
      padding: 8px 20px 4px;
    }
    .lead h2 {
      margin: 8px 0 6px;
      font-size: 20px;
      line-height: 26px;
      font-weight: 700;
    }
    .actions {
      display: grid;
      gap: 8px;
      padding: 12px 16px 0;
    }
    .center {
      display: flex;
      flex-direction: column;
      align-items: center;
      gap: 12px;
      padding: 32px 20px;
      color: var(--lk-muted);
    }
    .done-icon {
      font-size: 56px;
      color: var(--lk-primary);
    }
    ion-item > .step {
      display: grid;
      gap: 12px;
      width: 100%;
      padding: 14px 0;
    }
    .step__text {
      display: flex;
      gap: 12px;
      align-items: flex-start;
    }
    .step__num {
      flex: 0 0 auto;
      display: grid;
      place-items: center;
      width: 28px;
      height: 28px;
      border-radius: 50%;
      background: var(--lk-primary);
      color: var(--lk-on-primary);
      font-weight: 700;
      font-size: 15px;
    }
    .step__text p {
      margin: 3px 0 0;
      font-weight: 400;
      font-size: 16px;
      line-height: 22px;
      min-width: 0;
      overflow-wrap: anywhere;
    }
    /* Runs are elements, so the template's line breaks between them leave no stray spaces. */
    .step__text b {
      font-weight: 700;
    }
    .notes {
      margin: 4px 0 0;
      padding: 0 20px 0 36px;
      color: var(--lk-muted);
      font-size: 14px;
      line-height: 20px;
    }
    .notes li + li {
      margin-top: 6px;
    }
    .links {
      display: flex;
      flex-wrap: wrap;
      justify-content: space-between;
      gap: 8px 16px;
      padding: 24px 20px 0;
      font-size: 15px;
    }
  `,
  template: `
    <ion-content>
      <div class="wrap">
        <div class="brand">
          <img src="icons/icon.svg" alt="" />
          <h1>{{ desktop ? 'Встановити Лілейку' : 'Лілейка на телефоні' }}</h1>
          <p>
            Іконка на екрані, своє вікно без адресного рядка і сповіщення — як у звичайного
            застосунку. Без Play Market і App Store.
          </p>
        </div>

        @switch (view()) {
          @case ('installed') {
            <div class="lead" data-testid="install-installed">
              <h2>Лілейку вже встановлено</h2>
              <p>Ти відкрив її з головного екрана — більше нічого робити не треба.</p>
            </div>
            <div class="actions">
              <ion-button expand="block" routerLink="/" data-testid="install-open"
                >Відкрити Лілейку</ion-button
              >
            </div>
          }
          @case ('done') {
            <div class="center" data-testid="install-done">
              <ion-icon class="done-icon" name="checkmark-circle" aria-hidden="true" />
              <div class="lead">
                <h2>Готово!</h2>
                <p>Лілейка тепер серед застосунків. Відкривай її з іконки на екрані.</p>
              </div>
            </div>
            <div class="actions">
              <ion-button expand="block" fill="outline" routerLink="/">Продовжити тут</ion-button>
            </div>
          }
          @case ('prompt') {
            <div class="actions">
              <ion-button
                expand="block"
                size="large"
                (click)="installNow()"
                data-testid="install-now"
              >
                <ion-icon slot="start" name="download-outline" aria-hidden="true" />
                Встановити
              </ion-button>
            </div>
            <div class="lead">
              <p>Телефон спитає, чи додати Лілейку, — натисни «Встановити» ще раз у його вікні.</p>
            </div>
          }
          @case ('waiting') {
            <div class="center">
              <ion-spinner name="crescent" />
              <span>Готуємо встановлення…</span>
            </div>
          }
          @case ('manual') {
            @if (manual(); as m) {
              <div class="lead" data-testid="install-manual">
                <h2>{{ m.title }}</h2>
                @if (m.intro) {
                  <p>{{ m.intro }}</p>
                }
              </div>
              <ion-list [inset]="true">
                <ion-item-group>
                  @for (step of m.steps; track $index) {
                    <ion-item lines="full" data-testid="install-step">
                      <div class="step">
                        <div class="step__text">
                          <span class="step__num" aria-hidden="true">{{ $index + 1 }}</span>
                          <p>
                            @for (run of emphasize(step.text); track $index) {
                              @if (run.strong) {
                                <b>{{ run.text }}</b>
                              } @else {
                                <span>{{ run.text }}</span>
                              }
                            }
                          </p>
                        </div>
                        @if (step.art) {
                          <app-install-art [name]="step.art" />
                        }
                      </div>
                    </ion-item>
                  }
                </ion-item-group>
              </ion-list>
              @if (m.copyLink || m.openInChrome) {
                <div class="actions">
                  @if (m.openInChrome) {
                    <ion-button
                      expand="block"
                      [href]="chromeIntent"
                      data-testid="install-open-chrome"
                    >
                      <ion-icon slot="start" name="logo-chrome" aria-hidden="true" />
                      Відкрити в Chrome
                    </ion-button>
                  }
                  @if (m.copyLink) {
                    <ion-button
                      expand="block"
                      [fill]="m.openInChrome ? 'outline' : 'solid'"
                      (click)="copyLink()"
                      data-testid="install-copy"
                    >
                      <ion-icon slot="start" name="copy-outline" aria-hidden="true" />
                      Скопіювати посилання
                    </ion-button>
                  }
                </div>
              }
              @if (m.notes.length) {
                <ul class="notes">
                  @for (note of m.notes; track note) {
                    <li>
                      @for (run of emphasize(note); track $index) {
                        @if (run.strong) {
                          <b>{{ run.text }}</b>
                        } @else {
                          <span>{{ run.text }}</span>
                        }
                      }
                    </li>
                  }
                </ul>
              }
            }
          }
        }

        <div class="links">
          <a [href]="helpUrl" target="_blank" rel="noopener" data-testid="install-help"
            >Детальніше в довідці</a
          >
          @if (view() !== 'installed') {
            <a routerLink="/">Продовжити в браузері</a>
          }
          <a
            href="/"
            (click)="$event.preventDefault(); openFullVersion()"
            data-testid="install-full-version"
            >Повна версія</a
          >
        </div>
      </div>
    </ion-content>
  `,
})
export class InstallPage {
  private readonly install = inject(InstallService);
  private readonly toasts = inject(Toasts);

  private readonly guide = installGuide(currentInstallFacts());
  protected readonly desktop = this.guide.kind === 'desktop';
  protected readonly helpUrl = installHelpUrl();
  /** This page's own address, to paste into Safari or Chrome. */
  private readonly link = new URL('install', document.baseURI).href;
  /** Android hands an intent:// link to Chrome even from inside another app's web view. */
  protected readonly chromeIntent = `intent://${location.host}${new URL(this.link).pathname}#Intent;scheme=https;package=com.android.chrome;end`;

  private readonly waiting = signal(canOfferPrompt(this.guide));
  protected readonly emphasize = emphasize;

  protected readonly view = computed<View>(() => {
    if (this.guide.kind === 'installed' || this.install.standalone()) return 'installed';
    if (this.install.justInstalled()) return 'done';
    if (this.install.canPrompt()) return 'prompt';
    return this.waiting() ? 'waiting' : 'manual';
  });

  protected readonly manual = computed(() =>
    this.guide.kind === 'installed' ? null : manualInstall(this.guide),
  );

  constructor() {
    addIcons({ downloadOutline, checkmarkCircle, copyOutline, logoChrome });
    if (this.waiting()) {
      const timer = setTimeout(() => this.waiting.set(false), PROMPT_WAIT_MS);
      inject(DestroyRef).onDestroy(() => clearTimeout(timer));
    }
  }

  protected async installNow(): Promise<void> {
    const accepted = await this.install.prompt();
    // Said no: the steps by hand show, in case the person wants it from the menu after all.
    if (!accepted) this.waiting.set(false);
  }

  /** The web instead of the app, remembered (see web-links fullVersionUrl). */
  protected openFullVersion(): void {
    const url = fullVersionUrl();
    if (this.install.standalone()) openExternal(url);
    else location.assign(url);
  }

  protected async copyLink(): Promise<void> {
    try {
      await navigator.clipboard.writeText(this.link);
      await this.toasts.show('Посилання скопійовано');
    } catch {
      await this.toasts.show(`Не вдалося скопіювати. Посилання: ${this.link}`);
    }
  }
}
